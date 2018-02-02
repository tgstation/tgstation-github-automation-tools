using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
		/// The <see cref="IRequestManager"/> for the <see cref="TravisContinuousIntegration"/>
		/// </summary>
		readonly IRequestManager requestManager;
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
		/// <param name="travisConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="travisConfiguration"/></param>
		public TravisContinuousIntegration(IGitHubManager _gitHubManager, IOptions<TravisConfiguration> travisConfigurationOptions)
		{
			if (travisConfigurationOptions == null)
				throw new ArgumentNullException(nameof(travisConfigurationOptions));
			travisConfiguration = travisConfigurationOptions.Value;
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
		}

		/// <inheritdoc />
		public async Task<ContinuousIntegrationStatus> GetJobStatus(Octokit.Repository repository, PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var statuses = await gitHubManager.GetLatestCommitStatus(repository, pullRequest);
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
			var statuses = await gitHubManager.GetLatestCommitStatus(repository, pullRequest);
			var buildNumberRegex = new Regex(@"/builds/([1-9][0-9]*)\?");
			foreach (var I in statuses.Statuses)
			{
				if (!IsTravisStatus(I))
					continue;
				var buildNumber = buildNumberRegex.Match(I.TargetUrl).Groups[1].Value;
			}
		}
	}
}
