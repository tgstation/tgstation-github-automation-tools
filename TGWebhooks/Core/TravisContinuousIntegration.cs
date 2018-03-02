using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// <see cref="IContinuousIntegration"/> for Travis-CI
	/// </summary>
#pragma warning disable CA1812
	sealed class TravisContinuousIntegration : IContinuousIntegration
#pragma warning restore CA1812
	{
		/// <summary>
		/// Travis build api URL
		/// </summary>
		const string BaseBuildUrl = "https://api.travis-ci.org/build";

		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="TravisContinuousIntegration"/>
		/// </summary>
		readonly ILogger<TravisContinuousIntegration> logger;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="TravisContinuousIntegration"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IWebRequestManager"/> for the <see cref="TravisContinuousIntegration"/>
		/// </summary>
		readonly IWebRequestManager requestManager;
		/// <summary>
		/// The <see cref="TravisConfiguration"/> for the <see cref="TravisContinuousIntegration"/>
		/// </summary>
		readonly TravisConfiguration travisConfiguration;

		/// <summary>
		/// Runs a <paramref name="handler"/> for each travis status on a given <paramref name="pullRequest"/>. Does not run in paralled
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check for statuses</param>
		/// <param name="handler">A function taking a <see cref="CommitState"/> and travis build id <see cref="string"/> and returning <see langword="true"/> to continue, <see langword="false"/> to cancel</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task NonParallelForEachBuild(PullRequest pullRequest, Func<CommitState, string, bool> handler)
		{
			var statuses = await gitHubManager.GetLatestCommitStatus(pullRequest).ConfigureAwait(false);
			var buildNumberRegex = new Regex(@"/builds/([1-9][0-9]*)\?");
			foreach (var I in statuses.Statuses)
			{
				if (!I.TargetUrl.StartsWith("https://travis-ci.org/", StringComparison.InvariantCultureIgnoreCase))
				{
					logger.LogTrace("Skipping status #{0} as it is not a travis status", I.Id);
					continue;
				}

				var buildNumber = buildNumberRegex.Match(I.TargetUrl).Groups[1].Value;

				if (!handler(I.State.Value, buildNumber))
					break;
			}
		}

		/// <summary>
		/// Construct a <see cref="TravisContinuousIntegration"/>
		/// </summary>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="_requestManager">The value of <see cref="requestManager"/></param>
		/// <param name="travisConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="travisConfiguration"/></param>
		public TravisContinuousIntegration(ILogger<TravisContinuousIntegration> _logger, IGitHubManager _gitHubManager, IWebRequestManager _requestManager, IOptions<TravisConfiguration> travisConfigurationOptions)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			travisConfiguration = travisConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(travisConfigurationOptions));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			requestManager = _requestManager ?? throw new ArgumentNullException(nameof(_requestManager));
		}

		/// <summary>
		/// Get the headers required to use the Travis API
		/// </summary>
		/// <returns>A <see cref="List{T}"/> of <see cref="string"/> headers required to use the Travis API</returns>
		List<string> GetRequestHeaders()
		{
			return new List<string> { String.Format(CultureInfo.InvariantCulture, "User-Agent: {0}", Application.UserAgent), String.Format(CultureInfo.InvariantCulture, "Authorization: token {0}", travisConfiguration.APIToken), "Travis-API-Version: 3" };
		}

		/// <summary>
		/// Cancels and restarts a build
		/// </summary>
		/// <param name="buildNumber">The build number</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RestartBuild(string buildNumber, CancellationToken cancellationToken)
		{
			logger.LogDebug("Restarting build #{0}", buildNumber);
			var baseUrl = String.Join('/', BaseBuildUrl, buildNumber);
			Task DoBuildPost(string method) => requestManager.RunRequest(new Uri(String.Join('/', baseUrl, method)), String.Empty, GetRequestHeaders(), RequestMethod.POST, cancellationToken);
			try
			{
				//first ensure it's over
				await DoBuildPost("cancel").ConfigureAwait(false);
			}
			catch (WebException e)
			{
				//409 is what happens if the build isn't already running
				if (e.Status != WebExceptionStatus.ProtocolError || !(e.Response is HttpWebResponse response) || response.StatusCode != HttpStatusCode.Conflict)
					throw;					
			}
			//then restart it
			await DoBuildPost("restart").ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<ContinuousIntegrationStatus> GetJobStatus(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			logger.LogTrace("Getting job status for pull request #{0}", pullRequest.Number);
			var result = ContinuousIntegrationStatus.NotPresent;
			var tasks = new List<Task<bool>>();

			var cts = new CancellationTokenSource();
			var innerToken = cts.Token;
			using (cancellationToken.Register(() => cts.Cancel())) {

				await NonParallelForEachBuild(pullRequest, (state, buildNumber) => {
					if (state == CommitState.Failure)
					{
						result = ContinuousIntegrationStatus.Failed;
						return false;
					}
					if (state == CommitState.Error)
					{
						result = ContinuousIntegrationStatus.Errored;
						return false;
					}

					if (state == CommitState.Success)
					{
						//now determine if base is up to date
						result = ContinuousIntegrationStatus.Passed;

						async Task<bool> BuildIsUpToDate()
						{
							//https://developer.travis-ci.org/resource/build#Build
							var build = String.Join('/', BaseBuildUrl, buildNumber);
							var json = await requestManager.RunRequest(new Uri(build), null, GetRequestHeaders(), RequestMethod.GET, innerToken).ConfigureAwait(false);
							var jsonObj = JObject.Parse(json);
							var commitObj = (JObject)jsonObj["commit"];
							var sha = (string)commitObj["sha"];
							return sha == pullRequest.MergeCommitSha;
						};

						tasks.Add(BuildIsUpToDate());
					}
					else if (state == CommitState.Pending)
						result = ContinuousIntegrationStatus.Pending;
					return true;
				}).ConfigureAwait(false);

				try
				{
					await Task.WhenAll(tasks).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}

				if (result == ContinuousIntegrationStatus.Passed && tasks.Any(x => !x.Result))
					result = ContinuousIntegrationStatus.PassedOutdated;

				return result;
			}
		}

		/// <inheritdoc />
		public async Task TriggerJobRestart(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			logger.LogDebug("Restarting jobs for pull request #{0}", pullRequest.Number);
			var tasks = new List<Task>();
			await NonParallelForEachBuild(pullRequest, (status, buildNumber) => {
				tasks.Add(RestartBuild(buildNumber, cancellationToken));
				return true;
			}).ConfigureAwait(false);
			await Task.WhenAll(tasks).ConfigureAwait(false);
		}
	}
}
