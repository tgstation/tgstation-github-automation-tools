using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Core.Controllers;
using TGWebhooks.Core.Model;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class GitHubManager : IGitHubManager, IDisposable
#pragma warning restore CA1812
	{
		/// <summary>
		/// The scope required on Oauth tokens
		/// </summary>
		const string RequiredScope = "public_repo";
		/// <summary>
		/// The key on <see cref="dataStore"/> in which the <see cref="List{T}"/> of <see cref="AccessTokenEntry"/>s are stored
		/// </summary>
		const string AccessTokensKey = "AccessTokens";
		/// <summary>
		/// Days until a cookie for an <see cref="AccessTokenEntry"/> expires
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
		/// The <see cref="GitHubClient"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubClient gitHubClient;
		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly IDataStore dataStore;
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly ILogger logger;

		/// <summary>
		/// Used for controlled access of <see cref="knownUser"/>
		/// </summary>
		SemaphoreSlim semaphore;
		/// <summary>
		/// The <see cref="User"/> we are using the API with
		/// </summary>
		User knownUser;

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
		/// <param name="branchingDataStore">The <see cref="IBranchingDataStore"/> used to create <see cref="dataStore"/></param>
		public GitHubManager(IOptions<GeneralConfiguration> generalConfigurationOptions, IOptions<GitHubConfiguration> gitHubConfigurationOptions, IBranchingDataStore branchingDataStore, ILogger<GitHubManager> _logger)
		{
			gitHubConfiguration = gitHubConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			if (branchingDataStore == null)
				throw new ArgumentNullException(nameof(branchingDataStore));
			gitHubClient = new GitHubClient(new ProductHeaderValue(Application.UserAgent))
			{
				Credentials = new Credentials(gitHubConfiguration.PersonalAccessToken)
			};
			dataStore = branchingDataStore.BranchOnKey("GitHub");
			semaphore = new SemaphoreSlim(1);
		}

		/// <summary>
		/// Calls <see cref="IDisposable.Dispose"/> on <see cref="semaphore"/>
		/// </summary>
		public void Dispose()
		{
			semaphore.Dispose();
		}
		
		/// <summary>
		/// Sets <see cref="knownUser"/> if it is <see langword="null"/>
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CheckUser(bool doLock, CancellationToken cancellationToken)
		{
			if (!doLock)
			{
				if (knownUser == null)
					knownUser = await gitHubClient.User.Current().ConfigureAwait(false);
				return;
			}
			using (SemaphoreSlimContext.Lock(semaphore, cancellationToken))
				await CheckUser(false, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Get the <see cref="AccessTokenEntry"/>s which have not expired
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="List{T}"/> of active <see cref="AccessTokenEntry"/>s</returns>
		async Task<List<AccessTokenEntry>> GetTrimmedTokenEntries(CancellationToken cancellationToken)
		{
			var allEntries = await dataStore.ReadData<List<AccessTokenEntry>>(AccessTokensKey, cancellationToken).ConfigureAwait(false);
			var now = DateTimeOffset.Now;
			return allEntries.Where(x => x.Expiry < now).ToList();
		}

		/// <inheritdoc />
		public Task<PullRequest> GetPullRequest(int number)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Get pull request #{0}", number);
			return gitHubClient.PullRequest.Get(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<Label>> GetIssueLabels(int number)
		{
			logger.LogTrace("Get issue labels for #{0}", number);
			return gitHubClient.Issue.Labels.GetAllForIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task SetIssueLabels(int number, IEnumerable<string> newLabels)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Set issue labels for #{0}: {1}", number, newLabels);
			return gitHubClient.Issue.Labels.ReplaceAllForIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number, newLabels.ToArray());
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(int number)
		{
			IssueArgumentCheck(number);
			logger.LogTrace("Get changed files for #{0}", number);
			return gitHubClient.PullRequest.Files(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);
		}

		/// <inheritdoc />
		public Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			logger.LogTrace("Get latest commit status for #{0}", pullRequest.Number);
			return gitHubClient.Repository.Status.GetCombined(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Head.Sha);
		}

		/// <inheritdoc />
		public Task MergePullRequest(PullRequest pullRequest, string accessToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (accessToken == null)
				throw new ArgumentNullException(nameof(accessToken));

			logger.LogTrace("Merge #{0}", pullRequest.Number);
			var mergerClient = new GitHubClient(new ProductHeaderValue(Application.UserAgent))
			{
				Credentials = new Credentials(accessToken)
			};

			return mergerClient.PullRequest.Merge(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number, new MergePullRequest
			{
				CommitTitle = String.Format(CultureInfo.InvariantCulture, "{0} (#{1})", pullRequest.Title, pullRequest.Body),
				CommitMessage = pullRequest.Body,
				MergeMethod = PullRequestMergeMethod.Merge,
				Sha = pullRequest.Head.Sha
			});
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviews(PullRequest pullRequest)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			logger.LogTrace("Get reviews for #{0}", pullRequest.Number);
			return gitHubClient.PullRequest.Review.GetAll(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number);
		}

		/// <inheritdoc />
		public async Task<bool> UserHasWriteAccess(User user)
		{
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			logger.LogTrace("Check user write access for {0}", user.Login);
			var permissionLevel = await gitHubClient.Repository.Collaborator.ReviewPermission(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, user.Login).ConfigureAwait(false);
			var permission = permissionLevel.Permission.Value;
			return permission == PermissionLevel.Write || permission == PermissionLevel.Admin;
		}

		/// <inheritdoc />
		public Task<IReadOnlyList<PullRequest>> GetOpenPullRequests()
		{
			logger.LogTrace("Get open pull requests");
			return gitHubClient.PullRequest.GetAllForRepository(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, new PullRequestRequest
			{
				State = ItemStateFilter.Open
			});
		}

		/// <inheritdoc />
		public Task AddLabel(int number, string label)
		{
			logger.LogTrace("Add label {0} to #{1}", label, number);
			IssueArgumentCheck(number);
			if (label == null)
				throw new ArgumentNullException(nameof(label));
			
			return gitHubClient.Issue.Labels.AddToIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number, new string[] { label });
		}

		/// <inheritdoc />
		public Uri GetAuthorizationURL(Uri callbackURL)
		{
			logger.LogTrace("GetAuthorizationURL for {0}", callbackURL);
			var olr = new OauthLoginRequest(gitHubConfiguration.OauthClientID);
			olr.Scopes.Add(RequiredScope);  //all we need
			olr.RedirectUri = callbackURL;
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
			var result = await gitHubClient.Oauth.CreateAccessToken(otr).ConfigureAwait(false);
			if (!result.Scope.Contains(RequiredScope))
				//user is fucking with us, don't even bother
				return;

			var expiry = DateTime.Now.AddDays(AccessTokenCookieExpriationDays);

			var newEntry = new AccessTokenEntry()
			{
				Cookie = Guid.NewGuid(),
				AccessToken = result.AccessToken,
				Expiry = expiry
			};

			cookies.Append(newEntry.Cookie.ToString(), newEntry.AccessToken);
			var tokenEntries = await GetTrimmedTokenEntries(cancellationToken).ConfigureAwait(false);
			tokenEntries.Add(newEntry);
		}

		/// <inheritdoc />
		public Task CreateComment(int number, string body)
		{
			IssueArgumentCheck(number);
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			logger.LogTrace("Create comment: \"{0}\" on #{1}", body, number);

			return gitHubClient.Issue.Comment.Create(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number, body);
		}

		/// <inheritdoc />
		public Task ApprovePullRequest(PullRequest pullRequest, string approveMessage)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (approveMessage == null)
				throw new ArgumentNullException(nameof(approveMessage));

			logger.LogTrace("Approve #{0} with message: {1}", pullRequest.Number, approveMessage);

			return gitHubClient.PullRequest.Review.Create(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number, new PullRequestReviewCreate
			{
				Body = approveMessage,
				Event = PullRequestReviewEvent.Approve
			});
		}

		/// <inheritdoc />
		public async Task CreateSingletonComment(int number, string body, CancellationToken cancellationToken)
		{
			IssueArgumentCheck(number);
			if (body == null)
				throw new ArgumentNullException(nameof(body));

			logger.LogTrace("Create singleton comment: \"{0}\" on #{1}", body, number);
			var openComments = gitHubClient.Issue.Comment.GetAllForIssue(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, number);

			await CheckUser(true, cancellationToken).ConfigureAwait(false);

			foreach (var I in await openComments.ConfigureAwait(false))
				if (I.User.Id == knownUser.Id)
				{
					await gitHubClient.Issue.Comment.Update(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, I.Id, body).ConfigureAwait(false);
					return;
				}

			await CreateComment(number, body).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task DismissReview(PullRequest pullRequest, PullRequestReview pullRequestReview, string dismissMessage)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (pullRequestReview == null)
				throw new ArgumentNullException(nameof(pullRequestReview));
			if (dismissMessage == null)
				throw new ArgumentNullException(nameof(dismissMessage));

			logger.LogTrace("Dismiss review {0} on #{1}", pullRequestReview.Id, pullRequest.Number);
			return gitHubClient.PullRequest.Review.Dismiss(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, pullRequest.Number, pullRequestReview.Id, new PullRequestReviewDismiss
			{
				Message = dismissMessage
			});
		}

		/// <inheritdoc />
		public async Task<User> GetBotLogin(CancellationToken cancellationToken)
		{
			logger.LogTrace("Get bot login.");
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				await CheckUser(false, cancellationToken).ConfigureAwait(false);
				return knownUser;
			}
		}

		/// <inheritdoc />
		public Task SetCommitStatus(string commit, CommitState commitState, string description)
		{
			logger.LogTrace("SetCommitStatus for {0} to {1} with desc: {2}", commit, commitState, description);
			return gitHubClient.Repository.Status.Create(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName, commit, new NewCommitStatus()
			{
				Context = String.Concat(Application.UserAgent, '/', "status"),
				Description = description,
				State = commitState,
				TargetUrl = String.Concat(generalConfiguration.RootURL, StatusesController.Route)
			});
		}
	}
}
