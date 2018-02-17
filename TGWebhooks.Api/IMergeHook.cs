using Octokit;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Operations for modifying a <see cref="PullRequest"/> before it is merged
	/// </summary>
	public interface IMergeHook
	{
		/// <summary>
		/// Modify a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to modify</param>
		/// <param name="workingCommit">The commit SHA in the configured <see cref="IRepository"/> to work off of</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the modified <paramref name="workingCommit"/></returns>
		Task<string> ModifyMerge(PullRequest pullRequest, string workingCommit, CancellationToken cancellationToken);
	}
}