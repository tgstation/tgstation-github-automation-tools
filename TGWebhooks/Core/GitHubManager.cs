using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Configuration;
using TGWebhooks.Controllers;
using TGWebhooks.Models;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class GitHubManager : IGitHubManager
#pragma warning restore CA1812
	{
		/// <summary>
		/// The name cookie containing authorization keys
		/// </summary>
		const string CookieName = Application.UserAgent + ".AuthCookie";
		/// <summary>
		/// The scope required on Oauth tokens
		/// </summary>
		const string RequiredScope = "public_repo";
		/// <summary>
		/// Days until a cookie for an <see cref="UserAccessToken"/> expires
		/// </summary>
		const int AccessTokenCookieExpriationDays = 7;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;
		/// <summary>
		/// The <see cref="IGitHubClientFactory"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly IGitHubClientFactory gitHubClientFactory;
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly ILogger logger;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

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
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="gitHubConfiguration"/></param>
		/// <param name="_databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_gitHubClientFactory">The value of <see cref="gitHubClientFactory"/></param>
		public GitHubManager(IOptions<GeneralConfiguration> generalConfigurationOptions, IOptions<GitHubConfiguration> gitHubConfigurationOptions, IDatabaseContext _databaseContext, ILogger<GitHubManager> _logger, IGitHubClientFactory _gitHubClientFactory)
		{
			gitHubConfiguration = gitHubConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			databaseContext = _databaseContext ?? throw new ArgumentNullException(nameof(_databaseContext));
			gitHubClientFactory = _gitHubClientFactory ?? throw new ArgumentNullException(nameof(_gitHubClientFactory));
		}

		/// <inheritdoc />
		public async Task<PullRequest> GetPullRequest(long repositoryId, int number, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Get pull request #{0}", number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.PullRequest.Get(repositoryId, number).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int number, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Get pull request #{0}", number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repoOwner, repoName, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.PullRequest.Get(repoOwner, repoName, number).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<Label>> GetIssueLabels(long repositoryId, int number, CancellationToken cancellationToken)
		{
			logger.LogTrace("Get issue labels for #{0}", number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.Issue.Labels.GetAllForIssue(repositoryId, number).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task SetIssueLabels(long repositoryId, int number, IEnumerable<string> newLabels, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Set issue labels for #{0}: {1}", number, newLabels);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Issue.Labels.ReplaceAllForIssue(repositoryId, number, newLabels.ToArray()).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			logger.LogTrace("Get changed files for #{0}", pullRequest.Number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.PullRequest.Files(pullRequest.Base.Repository.Id, pullRequest.Number).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			logger.LogTrace("Get latest commit status for #{0}", pullRequest.Number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.Repository.Status.GetCombined(pullRequest.Base.Repository.Id, pullRequest.Head.Sha).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task MergePullRequest(PullRequest pullRequest, string accessToken, string sha)
		{
			Debugger.Break();

			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (accessToken == null)
				throw new ArgumentNullException(nameof(accessToken));
			if (sha == null)
				throw new ArgumentNullException(nameof(sha));

			logger.LogInformation("Merging pull request #{0} - {1}", pullRequest.Number, pullRequest.Title);
			var gitHubClient = gitHubClientFactory.CreateOauthClient(accessToken);

			return gitHubClient.PullRequest.Merge(pullRequest.Base.Repository.Id, pullRequest.Number, new MergePullRequest
			{
				CommitTitle = String.Format(CultureInfo.InvariantCulture, "{0} - #{1}", pullRequest.Title, pullRequest.Number),
				CommitMessage = pullRequest.Body,
				MergeMethod = PullRequestMergeMethod.Squash,
				Sha = sha
			});
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviews(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			logger.LogTrace("Get reviews for #{0}", pullRequest.Number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.PullRequest.Review.GetAll(pullRequest.Base.Repository.Id, pullRequest.Number).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<bool> UserHasWriteAccess(string repoOwner, string repoName, User user, CancellationToken cancellationToken)
		{
			if (repoOwner == null)
				throw new ArgumentNullException(nameof(repoOwner));
			if (repoName == null)
				throw new ArgumentNullException(nameof(repoName));
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			logger.LogTrace("Check user write access for {0}", user.Login);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repoOwner, repoName, cancellationToken).ConfigureAwait(false);
			var permissionLevel = await gitHubClient.Repository.Collaborator.ReviewPermission(repoOwner, repoName, user.Login).ConfigureAwait(false);
			var permission = permissionLevel.Permission.Value;
			return permission == PermissionLevel.Write || permission == PermissionLevel.Admin;
		}

		/// <inheritdoc />
		public async Task<IReadOnlyList<PullRequest>> GetOpenPullRequests(string repoOwner, string repoName, CancellationToken cancellationToken)
		{
			logger.LogTrace("Get open pull requests");
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repoOwner, repoName, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.PullRequest.GetAllForRepository(repoOwner, repoName, new PullRequestRequest
			{
				State = ItemStateFilter.Open
			}).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task AddLabel(long repositoryId, int number, string label, CancellationToken cancellationToken)
		{
			logger.LogTrace("Add label {0} to #{1}", label, number);
			IssueArgumentCheck(number);
			if (label == null)
				throw new ArgumentNullException(nameof(label));

			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Issue.Labels.AddToIssue(repositoryId, number, new string[] { label }).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Uri GetAuthorizationURL(Uri callbackURL)
		{
			logger.LogTrace("GetAuthorizationURL for {0}", callbackURL);
			var olr = new OauthLoginRequest(gitHubConfiguration.OauthClientID);
			olr.Scopes.Add(RequiredScope);  //all we need
			olr.RedirectUri = callbackURL;
			var gitHubClient = gitHubClientFactory.CreateAppClient();
			return gitHubClient.Oauth.GetGitHubLoginUrl(olr);
		}

		/// <inheritdoc />
		public async Task CompleteAuthorization(string code, IResponseCookies cookies, CancellationToken cancellationToken)
		{
			if (code == null)
				throw new ArgumentNullException(nameof(code));
			if (cookies == null)
				throw new ArgumentNullException(nameof(cookies));

			logger.LogTrace("CompleteAuthorization for with code: {0}", code);

			var otr = new OauthTokenRequest(gitHubConfiguration.OauthClientID, gitHubConfiguration.OauthSecret, code);
			var gitHubClient = gitHubClientFactory.CreateAppClient();
			var result = await gitHubClient.Oauth.CreateAccessToken(otr).ConfigureAwait(false);
			if (result.AccessToken == null || !result.Scope.Contains(RequiredScope))
				//user is fucking with us, don't even bother
				return;

			var expiry = DateTime.Now.AddDays(AccessTokenCookieExpriationDays);

			var newEntry = new UserAccessToken()
			{
				Id = Guid.NewGuid(),
				AccessToken = result.AccessToken,
				Expiry = expiry
			};

			cookies.Append(CookieName, newEntry.Id.ToString(), new CookieOptions{
				SameSite = SameSiteMode.Strict,
				Secure = true,
				Expires = expiry
			});
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				await databaseContext.UserAccessTokens.AddAsync(newEntry, cancellationToken).ConfigureAwait(false);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task CreateComment(long repositoryId, int number, string body, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			logger.LogTrace("Create comment: \"{0}\" on #{1}", body, number);

			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Issue.Comment.Create(repositoryId, number, body).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ApprovePullRequest(PullRequest pullRequest, string approveMessage, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (approveMessage == null)
				throw new ArgumentNullException(nameof(approveMessage));

			logger.LogTrace("Approve #{0} with message: {1}", pullRequest.Number, approveMessage);

			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			await gitHubClient.PullRequest.Review.Create(pullRequest.Base.Repository.Id, pullRequest.Number, new PullRequestReviewCreate
			{
				Body = approveMessage,
				Event = PullRequestReviewEvent.Approve
			}).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task CreateSingletonComment(long repositoryId, int number, string body, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);

			var openComments = gitHubClient.Issue.Comment.GetAllForIssue(repositoryId, number);

			var user = await gitHubClient.User.Current().ConfigureAwait(false);

			foreach (var I in await openComments.ConfigureAwait(false))
				if (I.User.Id == user.Id)
				{
					await gitHubClient.Issue.Comment.Update(repositoryId, I.Id, body).ConfigureAwait(false);
					return;
				}

			await gitHubClient.Issue.Comment.Create(repositoryId, number, body).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task DismissReview(PullRequest pullRequest, PullRequestReview pullRequestReview, string dismissMessage, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (pullRequestReview == null)
				throw new ArgumentNullException(nameof(pullRequestReview));
			if (dismissMessage == null)
				throw new ArgumentNullException(nameof(dismissMessage));

			logger.LogTrace("Dismiss review {0} on #{1}", pullRequestReview.Id, pullRequest.Number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			await gitHubClient.PullRequest.Review.Dismiss(pullRequest.Base.Repository.Id, pullRequest.Number, pullRequestReview.Id, new PullRequestReviewDismiss
			{
				Message = dismissMessage
			}).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task<User> GetUser(string accessToken)
		{
			var gitHubClient = accessToken != null ? gitHubClientFactory.CreateOauthClient(accessToken) : gitHubClientFactory.CreateAppClient();
			return gitHubClient.User.Current();
		}

		/// <inheritdoc />
		public async Task SetCommitStatus(PullRequest pullRequest, CommitState commitState, string description, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (description == null)
				throw new ArgumentNullException(nameof(description));
			var commit = pullRequest.Head.Sha;
			logger.LogTrace("SetCommitStatus for {0} to {1} with desc: {2}", commit, commitState, description);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Repository.Status.Create(pullRequest.Base.Repository.Id, commit, new NewCommitStatus()
			{
				Context = String.Concat(Application.UserAgent, '/', "status"),
				Description = description,
				State = commitState,
				TargetUrl = String.Join('/', generalConfiguration.RootURL, PullRequestController.Route, pullRequest.Number)
			}).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public void ExpireAuthorization(IResponseCookies cookies) => cookies.Delete(CookieName);

		/// <inheritdoc />
		public async Task<string> CheckAuthorization(string repoOwner, string repoName, IRequestCookieCollection cookies, CancellationToken cancellationToken)
		{
			if (!cookies.TryGetValue(CookieName, out string cookieGuid))
				return null;

			if (!Guid.TryParse(cookieGuid, out Guid guid))
				return null;

			//cleanup
			var now = DateTimeOffset.Now;

			//two queries is better here
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				var toRemove = await databaseContext.UserAccessTokens.Where(x => x.Expiry < now).ToAsyncEnumerable().ToList().ConfigureAwait(false);
				databaseContext.UserAccessTokens.RemoveRange(toRemove);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);

				var entry = await databaseContext.UserAccessTokens.Where(x => x.Id == guid && x.Expiry >= now).ToAsyncEnumerable().FirstOrDefault().ConfigureAwait(false);
				if (entry == default(UserAccessToken))
					return null;

				return entry.AccessToken;
			}
		}

		/// <inheritdoc />
		public async Task Close(long repositoryId, int number, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Issue.Update(repositoryId, number, new IssueUpdate { State = ItemState.Closed }).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task DeleteBranch(long repositoryId, string branchName, CancellationToken cancellationToken)
		{
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Git.Reference.Delete(repositoryId, String.Format(CultureInfo.InvariantCulture, "heads/{0}", branchName)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task CreateFile(long repositoryId, string branchName, string commitMessage, string path, string content, CancellationToken cancellationToken)
		{
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			await gitHubClient.Repository.Content.CreateFile(repositoryId, path, new CreateFileRequest(commitMessage, content, branchName, true)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Commit> GetCommit(long repositoryId, string reference, CancellationToken cancellationToken)
		{
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(repositoryId, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.Git.Commit.Get(repositoryId, reference).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Repository> GetRepository(string owner, string name, CancellationToken cancellationToken)
		{
			var gitHubClient = await gitHubClientFactory.CreateInstallationClient(owner, name, cancellationToken).ConfigureAwait(false);
			return await gitHubClient.Repository.Get(owner, name).ConfigureAwait(false);
		}
	}
}
