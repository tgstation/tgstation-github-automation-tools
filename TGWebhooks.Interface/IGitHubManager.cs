using Octokit;
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
		/// <param name="repository">The <see cref="Repository"/> the <see cref="Issue"/> is from</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="Label"/>s</returns>
		Task<IReadOnlyList<Label>> GetIssueLabels(Repository repository, int number);

		/// <summary>
		/// Get all <see cref="Label"/>s for an <see cref="Issue"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the <see cref="Issue"/> is from</param>
		/// <param name="number">The number of the <see cref="Issue"/></param>
		/// <param name="newLabels">The new labels apply</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetIssueLabels(Repository repository, int number, IEnumerable<string> newLabels);

		/// <summary>
		/// Get a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the <see cref="PullRequest"/> is from</param>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="PullRequest"/></returns>
		Task<PullRequest> GetPullRequest(Repository repository, int number);

		/// <summary>
		/// Get the <see cref="CommitStatus"/>es for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the <paramref name="pullRequest"/> is from</param>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="CombinedCommitStatus"/> for the latest <see cref="Commit"/> of a <paramref name="pullRequest"/></returns>
		Task<CombinedCommitStatus> GetLatestCommitStatus(Repository repository, PullRequest pullRequest);

		/// <summary>
		/// Get the files changed by a <see cref="PullRequest"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the pull request is from</param>
		/// <param name="number">The number of the <see cref="PullRequest"/></param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IReadOnlyList{T}"/> of <see cref="PullRequestFile"/>s</returns>
		Task<IReadOnlyList<PullRequestFile>> GetPullRequestChangedFiles(Repository repository, int number);
	}
}
