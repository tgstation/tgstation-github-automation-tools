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
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="Label"/>s</returns>
		Task<IReadOnlyList<Label>> GetIssueLabels(long repositoryId, int number, CancellationToken cancellationToken);

		/// <summary>
		/// Get all <see cref="Label"/>s for an <see cref="Issue"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="newLabels">The new labels apply</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetIssueLabels(long repositoryId, int number, IEnumerable<string> newLabels, CancellationToken cancellationToken);

		/// <summary>
		/// Get a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="PullRequest"/></returns>
		Task<PullRequest> GetPullRequest(long repositoryId, int number, CancellationToken cancellationToken);

		/// <summary>
		/// Get a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="repoOwner">The <see cref="Repository.Owner"/></param>
		/// <param name="repoName">The <see cref="Repository.Name"/></param>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="PullRequest"/></returns>
		Task<PullRequest> GetPullRequest(string repoOwner, string repoName, int number, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all open <see cref="PullRequest"/>s
		/// </summary>
		/// <param name="repoOwner">The <see cref="Repository.Owner"/></param>
		/// <param name="repoName">The <see cref="Repository.Name"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of open <see cref="PullRequest"/>s</returns>
		Task<IReadOnlyList<PullRequest>> GetOpenPullRequests(string repoOwner, string repoName, CancellationToken cancellationToken);

		/// <summary>
		/// Get the <see cref="CommitStatus"/>es for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedCommitStatus"/> for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/></returns>
		Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a <see cref="Repository"/>
		/// </summary>
		/// <param name="owner">The <see cref="Repository.Owner"/></param>
		/// <param name="name">The <see cref="Repository.Name"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Repository"/></returns>
		Task<Repository> GetRepository(string owner, string name, CancellationToken cancellationToken);

		/// <summary>
		/// Close the specified <see cref="Issue"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The <see cref="Issue.Number"/> to close</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Close(long repositoryId, int number, CancellationToken cancellationToken);

		/// <summary>
		/// Get the files changed by a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestFile"/>s</returns>
		Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(PullRequest pullRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Squashes and merges the given <see cref="PullRequest"/> with it's current <see cref="PullRequest.Title"/>, <see cref="PullRequest.Number"/>, and <see cref="PullRequest.Body"/> as the log message
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to merge</param>
		/// <param name="accessToken">The access token to merge with</param>
		/// <param name="sha">The <see cref="GitReference.Sha"/> to accept the merge of</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MergePullRequest(PullRequest pullRequest, string accessToken, string sha);

		/// <summary>
		/// Get all <see cref="PullRequestReview"/>s for a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to get reviews for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestReview"/>s for <paramref name="pullRequest"/></returns>
		Task<IReadOnlyList<PullRequestReview>> GetPullRequestReviews(PullRequest pullRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Check if a <paramref name="user"/> has write access to the configured repository
		/// </summary>
		/// <param name="repoOwner">The <see cref="Repository.Owner"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="repoName">The <see cref="Repository.Name"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="user">The <see cref="User"/> to check access for</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the <paramref name="user"/> has write access, <see langword="false"/> otherwise</returns>
		Task<bool> UserHasWriteAccess(string repoOwner, string repoName, User user, CancellationToken cancellationToken);

		/// <summary>
		/// Adds a <paramref name="label"/> to a given <see cref="Issue"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The <see cref="Issue.Number"/> of the <see cref="Issue"/> to label</param>
		/// <param name="label">The label to add</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task AddLabel(long repositoryId, int number, string label, CancellationToken cancellationToken);

		/// <summary>
		/// Get the GitHub URL to direct a user to at the start of the Oauth flow
		/// </summary>
		/// <param name="callbackURL">The <see cref="Uri"/> to direct users to to complete the Oauth flow</param>
		/// <param name="repoOwner">The <see cref="Repository.Owner"/> for the operation</param>
		/// <param name="repoName">The <see cref="Repository.Name"/> for the operation</param>
		/// <param name="number">The <see cref="PullRequest.Number"/> to return to on the redirect</param>
		/// <returns>The <see cref="Uri"/> to send the user to</returns>
		Uri GetAuthorizationURL(Uri callbackURL, string repoOwner, string repoName, int number);

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
		/// <param name="repoOwner">The <see cref="Repository.Owner"/> for the operation</param>
		/// <param name="repoName">The <see cref="Repository.Name"/> for the operation</param>
		/// <param name="cookies">The <see cref="IRequestCookieCollection"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the associated GitHub access token on success, <see langword="null"/> on failure</returns>
		Task<string> CheckAuthorization(string repoOwner, string repoName, IRequestCookieCollection cookies, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a comment on the specified <see cref="Issue"/>, or updates the first one if it has already done so
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="body">The body of the comment</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateSingletonComment(long repositoryId, int number, string body, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a comment on the specified <see cref="Issue"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="body">The body of the comment</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateComment(long repositoryId, int number, string body, CancellationToken cancellationToken);

		/// <summary>
		/// Gets a specified <see cref="Commit"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="sha">The <see cref="GitReference.Ref"/> of the <see cref="Commit"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Commit"/></returns>
		Task<Commit> GetCommit(long repositoryId, string sha, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an "Approved" review on <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to approve</param>
		/// <param name="approveMessage">The message to approve with</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ApprovePullRequest(PullRequest pullRequest, string approveMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Dismiss a <paramref name="pullRequestReview"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to dismiss the review of</param>
		/// <param name="pullRequestReview">The <see cref="PullRequestReview"/> to dismiss</param>
		/// <param name="dismissMessage">The message to dismiss with</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DismissReview(PullRequest pullRequest, PullRequestReview pullRequestReview, string dismissMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Creates a file on a given <paramref name="branch"/> at a given <paramref name="path"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="branch">The branch to push to</param>
		/// <param name="commitMessage">The commit message for the push</param>
		/// <param name="path">The path in the <see cref="Repository"/> to create the file at</param>
		/// <param name="content">The content of the file</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task CreateFile(long repositoryId, string branch, string commitMessage, string path, string content, CancellationToken cancellationToken);

		/// <summary>
		/// Get the GitHub <see cref="User"/> of either the configured or specified <paramref name="accessToken"/>
		/// </summary>
		/// <param name="accessToken">Optional GitHub access token to get the user for</param>
		/// <returns>A <see cref="Task{TResult}"/> the configured <see cref="User"/></returns>
		Task<User> GetUser(string accessToken);

		/// <summary>
		/// Creates a <see cref="CommitStatus"/> for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> whose SHA of the HEAD commit to set the <see cref="CommitStatus"/> for</param>
		/// <param name="commitState">The <see cref="CommitState"/> of the commit</param>
		/// <param name="description">A description of the <see cref="CommitStatus"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetCommitStatus(PullRequest pullRequest, CommitState commitState, string description, CancellationToken cancellationToken);

		/// <summary>
		/// Deletes a given <see cref="Branch"/> on the target repository
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> of the <see cref="Repository"/> to operate on</param>
		/// <param name="branchName">The name of the <see cref="Branch"/> to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteBranch(long repositoryId, string branchName, CancellationToken cancellationToken);
	}
}
