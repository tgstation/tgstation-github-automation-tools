using Octokit;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Represents a bar on automatic merging of a pull request
	/// </summary>
	public interface IMergeRequirement
	{
		/// <summary>
		/// The user friendly name of the <see cref="IMergeRequirement"/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// A description of the <see cref="Description"/>
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Run the <see cref="IMergeRequirement"/> for a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="AutoMergeStatus"/> of <paramref name="pullRequest"/></returns>
		Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken);
	}
}
