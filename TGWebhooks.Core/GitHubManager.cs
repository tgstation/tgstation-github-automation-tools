using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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

		static void IssueArgumentCheck(Octokit.Repository repository, int number)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));

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
		public Task<PullRequest> GetPullRequest(Octokit.Repository repository, int number)
		{
			IssueArgumentCheck(repository, number);
			return gitHubClient.PullRequest.Get(repository.Id, number);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<Label>> GetIssueLabels(Octokit.Repository repository, int number)
		{
			IssueArgumentCheck(repository, number);
			return gitHubClient.Issue.Labels.GetAllForIssue(repository.Id, number);
		}

		/// <inheritdoc />
		public Task SetIssueLabels(Octokit.Repository repository, int number, IEnumerable<string> newLabels)
		{
			IssueArgumentCheck(repository, number);
			return gitHubClient.Issue.Labels.ReplaceAllForIssue(repository.Id, number, newLabels.ToArray());
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(Octokit.Repository repository, int number)
		{
			IssueArgumentCheck(repository, number);
			return gitHubClient.PullRequest.Files(repository.Id, number);
		}

		/// <inheritdoc />
		public Task<CombinedCommitStatus> GetLatestCommitStatus(Octokit.Repository repository, PullRequest pullRequest)
		{
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			return gitHubClient.Repository.Status.GetCombined(repository.Id, pullRequest.Head.Sha);
		}
	}
}
