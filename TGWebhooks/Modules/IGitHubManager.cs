using Microsoft.AspNetCore.Http;
using Octokit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Access to the GitHub API
	/// </summary>
	public interface IGitHubManager
	{
		/// <summary>
		/// Get all <see cref="Label"/>s for an <see cref="Issue"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="Label"/>s</returns>
		Task<IReadOnlyList<Label>> GetIssueLabels(int number);

		/// <summary>
		/// Get all <see cref="Label"/>s for an <see cref="Issue"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="newLabels">The new labels apply</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetIssueLabels(int number, IEnumerable<string> newLabels);

		/// <summary>
		/// Get a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="PullRequest"/></returns>
		Task<PullRequest> GetPullRequest(int number);

		/// <summary>
		/// Gets all open <see cref="PullRequest"/>s
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of open <see cref="PullRequest"/>s</returns>
		Task<IReadOnlyList<PullRequest>> GetOpenPullRequests();

		/// <summary>
		/// Get the <see cref="CommitStatus"/>es for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedCommitStatus"/> for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/></returns>
		Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest);

		/// <summary>
		/// Close the specified <see cref="Issue"/>
		/// </summary>
		/// <param name="number">The <see cref="Issue.Number"/> to close</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Close(int number);

		/// <summary>
		/// Get the files changed by a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestFile"/>s</returns>
		Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(int number);

		/// <summary>
		/// Squashes and merges the given <see cref="PullRequest"/> with it's current <see cref="PullRequest.Title"/>, <see cref="PullRequest.Number"/>, and <see cref="PullRequest.Body"/> as the log message
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to merge</param>
		/// <param name="overrideAccessToken">The access token to merge with</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MergePullRequest(PullRequest pullRequest, string overrideAccessToken);

		/// <summary>
		/// Get all <see cref="PullRequestReview"/>s for a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to get reviews for</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestReview"/>s for <paramref name="pullRequest"/></returns>
		Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviews(PullRequest pullRequest);

		/// <summary>
		/// Check if a <paramref name="user"/> has write access to the configured repository
		/// </summary>
		/// <param name="user">The <see cref="User"/> to check access for</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <paramref name="user"/> has write access, <see langword="false"/> otherwise</returns>
		Task<bool> UserHasWriteAccess(User user);

		/// <summary>
		/// Adds a <paramref name="label"/> to a given <see cref="Issue"/>
		/// </summary>
		/// <param name="number">The <see cref="Issue.Number"/> of the <see cref="Issue"/> to label</param>
		/// <param name="label">The label to add</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task AddLabel(int number, string label);

		/// <summary>
		/// Get the GitHub URL to direct a user to at the start of the Oauth flow
		/// </summary>
		/// <param name="callbackURL">The <see cref="Uri"/> to direct users to to complete the Oauth flow</param>
		/// <returns>The <see cref="Uri"/> to send the user to</returns>
		Uri GetAuthorizationURL(Uri callbackURL);

		/// <summary>
		/// Complete the Oauth flow and load 
		/// </summary>
		/// <param name="code">The code entry in the recieved JSON from an Oauth redirect</param>
		/// <param name="cookies">The <see cref="IResponseCookies"/> to write session information to</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CompleteAuthorization(string code, IResponseCookies cookies, CancellationToken cancellationToken);

		/// <summary>
		/// Expire an oauth cookie
		/// </summary>
		/// <param name="cookies">The <see cref="IResponseCookies"/> to write session information to</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		void ExpireAuthorization(IResponseCookies cookies);

		/// <summary>
		/// Checks some <paramref name="cookies"/> for the oauth cookie
		/// </summary>
		/// <param name="cookies">The <see cref="IRequestCookieCollection"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the associated GitHub access token on success, <see langword="null"/> on failure</returns>
		Task<string> CheckAuthorization(IRequestCookieCollection cookies, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a comment on the specified <see cref="Issue"/>, or updates the first one if it has already done so
		/// </summary>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="body">The body of the comment</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateSingletonComment(int number, string body, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a comment on the specified <see cref="Issue"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="body">The body of the comment</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateComment(int number, string body);

		/// <summary>
		/// Creates an "Approved" review on <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to approve</param>
		/// <param name="approveMessage">The message to approve with</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ApprovePullRequest(PullRequest pullRequest, string approveMessage);

		/// <summary>
		/// Dismiss a <paramref name="pullRequestReview"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to dismiss the review of</param>
		/// <param name="pullRequestReview">The <see cref="PullRequestReview"/> to dismiss</param>
		/// <param name="dismissMessage">The message to dismiss with</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DismissReview(PullRequest pullRequest, PullRequestReview pullRequestReview, string dismissMessage);

		/// <summary>
		/// Get the GitHub <see cref="User"/> of either the configured or specified <paramref name="accessToken"/>
		/// </summary>
		/// <param name="accessToken">Optional GitHub access token to get the user for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> the configured <see cref="User"/></returns>
		Task<User> GetUserLogin(string accessToken, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a <see cref="CommitStatus"/> for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> whose SHA of the HEAD commit to set the <see cref="CommitStatus"/> for</param>
		/// <param name="commitState">The <see cref="CommitState"/> of the commit</param>
		/// <param name="description">A description of the <see cref="CommitStatus"/></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetCommitStatus(PullRequest pullRequest, CommitState commitState, string description);
	}
}
