using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
    sealed class GitHubManager : IGitHubManager
    {
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;
		/// <summary>
		/// The <see cref="GitHubClient"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubClient gitHubClient;

		/// <summary>
		/// Validate an <see cref="Issue"/> <paramref name="number"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="Issue"/> to validate</param>
		static void IssueArgumentCheck(int number)
		{
			if (number < 1)
				throw new ArgumentOutOfRangeException(nameof(number), number, String.Format(CultureInfo.CurrentCulture, "{0} must be greater than zero!", nameof(number)));
		}

		/// <summary>
		/// Construct a <see cref="GitHubManager"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="gitHubConfiguration"/></param>
		public GitHubManager(IOptions<GitHubConfiguration> gitHubConfigurationOptions)
		{
			if(gitHubConfigurationOptions == null)
				throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			gitHubConfiguration = gitHubConfigurationOptions.Value;
			gitHubClient = new GitHubClient(new ProductHeaderValue(Application.UserAgent))
			{
				Credentials = new Credentials(gitHubConfiguration.PersonalAccessToken)
			};
		}

		/// <inheritdoc />
		public Task<PullRequest> GetPullRequest(int number)
		{
			IssueArgumentCheck(number);
			return gitHubClient.PullRequest.Get(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<Label>> GetIssueLabels(int number)
		{
			return gitHubClient.Issue.Labels.GetAllForIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task SetIssueLabels(int number, IEnumerable<string> newLabels)
		{
			IssueArgumentCheck(number);
			return gitHubClient.Issue.Labels.ReplaceAllForIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number, newLabels.ToArray());
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(int number)
		{
			IssueArgumentCheck(number);
			return gitHubClient.PullRequest.Files(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			return gitHubClient.Repository.Status.GetCombined(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Head.Sha);
		}
	}
}
