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
		readonly GitHubConfiguration configuration;
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
		/// <param name="_configuration">The value of <see cref="configuration"/></param>
		public GitHubManager(GitHubConfiguration _configuration)
		{
			configuration = _configuration ?? throw new ArgumentNullException(nameof(_configuration));
			gitHubClient = new GitHubClient(new ProductHeaderValue("tgstation-github-automation-tools"))
			{
				Credentials = new Credentials(configuration.PersonalAccessToken)
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
	}
}
