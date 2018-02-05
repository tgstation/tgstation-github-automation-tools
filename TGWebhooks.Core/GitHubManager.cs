using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Core.Model;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
    sealed class GitHubManager : IGitHubManager
    {
		/// <summary>
		/// The client ID of the oauth client
		/// </summary>
		const string OauthClientID = "57a7187d56c2f51ce60d";

		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;
		/// <summary>
		/// The <see cref="GitHubClient"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubClient gitHubClient;
		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly IDataStore dataStore;

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
		public GitHubManager(IOptions<GitHubConfiguration> gitHubConfigurationOptions, IBranchingDataStore branchingDataStore)
		{
			if(gitHubConfigurationOptions == null)
				throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			if (branchingDataStore == null)
				throw new ArgumentNullException(nameof(branchingDataStore));
			gitHubConfiguration = gitHubConfigurationOptions.Value;
			gitHubClient = new GitHubClient(new ProductHeaderValue(Application.UserAgent))
			{
				Credentials = new Credentials(gitHubConfiguration.PersonalAccessToken)
			};
			dataStore = branchingDataStore.BranchOnKey("GitHub");
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

		/// <inheritdoc />
		public Task MergePullRequest(PullRequest pullRequest)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			
			return gitHubClient.PullRequest.Merge(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number, new MergePullRequest
			{
				CommitTitle = String.Format(CultureInfo.InvariantCulture, "{0} (#{1})", pullRequest.Title, pullRequest.Body),
				CommitMessage = pullRequest.Body,
				MergeMethod = PullRequestMergeMethod.Squash,
				Sha = pullRequest.Head.Sha
			});
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviews(PullRequest pullRequest)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			
			return gitHubClient.PullRequest.Review.GetAll(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number);
		}

		/// <inheritdoc />
		public async Task<bool> UserHasWriteAccess(User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			var permissionLevel = await gitHubClient.Repository.Collaborator.ReviewPermission(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, user.Login);
			var permission = permissionLevel.Permission.Value;
			return permission == PermissionLevel.Write || permission == PermissionLevel.Admin;
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequest>> GetOpenPullRequests()
		{
			return gitHubClient.PullRequest.GetAllForRepository(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, new PullRequestRequest
			{
				State = ItemStateFilter.Open
			});
		}

		/// <inheritdoc />
		public Task AddLabel(int number, string label)
		{
			IssueArgumentCheck(number);
			if (label == null)
				throw new ArgumentNullException(nameof(label));
			
			return gitHubClient.Issue.Labels.AddToIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number, new string[] { label });
		}

		public Uri GetAuthorizationURL(Uri callbackURL)
		{
			var olr = new OauthLoginRequest(gitHubConfiguration.OauthClientID);
			olr.Scopes.Add("public_repo");  //all we need
			olr.RedirectUri = callbackURL;
			return gitHubClient.Oauth.GetGitHubLoginUrl(olr);
		}

		public Task CompleteAuthorization(string code)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));
			var otr = new OauthTokenRequest(gitHubConfiguration.OauthClientID, gitHubConfiguration.OauthSecret, code);
			return gitHubClient.Oauth.CreateAccessToken(otr);
		}
	}
}
