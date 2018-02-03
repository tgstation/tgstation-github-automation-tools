﻿using Octokit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
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
		/// Get the <see cref="CommitStatus"/>es for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedCommitStatus"/> for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/></returns>
		Task<CombinedCommitStatus> GetLatestCommitStatus(PullRequest pullRequest);

		/// <summary>
		/// Get the files changed by a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestFile"/>s</returns>
		Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(int number);

		/// <summary>
		/// Squashes and merges the given <see cref="PullRequest"/> with it's current <see cref="PullRequest.Title"/>, <see cref="PullRequest.Number"/>, and <see cref="PullRequest.Body"/> as the log message
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task MergePullRequest(PullRequest pullRequest);

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
	}
}
