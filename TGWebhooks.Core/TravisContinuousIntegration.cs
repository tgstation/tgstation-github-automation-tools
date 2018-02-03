using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// <see cref="IContinuousIntegration"/> for Travis-CI
	/// </summary>
	sealed class TravisContinuousIntegration : IContinuousIntegration
	{
		/// <inheritdoc />
		public string Name => "Travis-CI";

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
		/// Checks if a given <paramref name="commitStatus"/> is for Travis-CI
		/// </summary>
		/// <param name="commitStatus">The <see cref="CommitStatus"/> to check</param>
		/// <returns><see langword="true"/> if the <paramref name="commitStatus"/> is for Travis-CI, <see langword="false"/> otherwise</returns>
		static bool IsTravisStatus(CommitStatus commitStatus)
		{
			return commitStatus.TargetUrl.StartsWith("https://travis-ci.org/");
		}

		/// <summary>
		/// Construct a <see cref="TravisContinuousIntegration"/>
		/// </summary>
		/// <param name="_gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="_requestManager">The value of <see cref="requestManager"/></param>
		/// <param name="travisConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="travisConfiguration"/></param>
		public TravisContinuousIntegration(IGitHubManager _gitHubManager, IWebRequestManager _requestManager, IOptions<TravisConfiguration> travisConfigurationOptions)
		{
			if (travisConfigurationOptions == null)
				throw new ArgumentNullException(nameof(travisConfigurationOptions));
			travisConfiguration = travisConfigurationOptions.Value;
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			requestManager = _requestManager ?? throw new ArgumentNullException(nameof(_requestManager));
		}

		/// <summary>
		/// Get the headers required to use the Travis API
		/// </summary>
		/// <returns>A <see cref="List{T}"/> of <see cref="string"/> headers required to use the Travis API</returns>
		List<string> GetRequestHeaders()
		{
			return new List<string> { String.Format(CultureInfo.InvariantCulture, "User-Agent: {0}", Application.UserAgent), String.Format(CultureInfo.InvariantCulture, "Authorization: token {0}", travisConfiguration.APIToken) };
		}

		/// <summary>
		/// Cancels and restarts a build
		/// </summary>
		/// <param name="buildNumber">The build number</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RestartBuild(string buildNumber, CancellationToken cancellationToken)
		{
			const string baseBuildURL = "https://api.travis-ci.org/build";
			var baseUrl = String.Join('/', baseBuildURL, buildNumber);
			Task DoBuildPost(string method)
			{
				return requestManager.RunRequest(String.Join('/', baseUrl, method), String.Empty, GetRequestHeaders(), RequestMethod.POST, cancellationToken);
			}
			//first ensure it's over
			await DoBuildPost("cancel");
			//then restart it
			await DoBuildPost("restart");
		}

		/// <inheritdoc />
		public async Task<ContinuousIntegrationStatus> GetJobStatus(Octokit.Repository repository, PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var statuses = await gitHubManager.GetLatestCommitStatus(pullRequest);
			var result = ContinuousIntegrationStatus.NotPresent;
			foreach(var I in statuses.Statuses)
			{
				if (!IsTravisStatus(I))
					continue;
				else if (result == ContinuousIntegrationStatus.NotPresent)
					result = ContinuousIntegrationStatus.Passed;
				if (I.State == "error")
					return ContinuousIntegrationStatus.Failed;
				if (I.State == "pending")
					result = ContinuousIntegrationStatus.Pending;
			}
			return result;
		}

		/// <inheritdoc />
		public async Task TriggerJobRestart(Octokit.Repository repository, PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var statuses = await gitHubManager.GetLatestCommitStatus(pullRequest);
			var buildNumberRegex = new Regex(@"/builds/([1-9][0-9]*)\?");
			var tasks = new List<Task>();
			foreach (var I in statuses.Statuses)
			{
				if (!IsTravisStatus(I))
					continue;
				var buildNumber = buildNumberRegex.Match(I.TargetUrl).Groups[1].Value;

				tasks.Add(RestartBuild(buildNumber, cancellationToken));
			}
			await Task.WhenAll(tasks);
		}
	}
}
