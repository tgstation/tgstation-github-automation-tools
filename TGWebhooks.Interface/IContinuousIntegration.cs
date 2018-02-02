using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Describes continuous integration status for a <see cref="Octokit.PullRequest"/>
	/// </summary>
	interface IContinuousIntegration
    {
		/// <summary>
		/// Checks the job status for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		/// <param name="pullRequestNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the job has passed, <see langword="false"/> if it failed, <see langword="null"/> if it's in progress</returns>
		Task<bool?> GetJobStatus(int pullRequestNumber, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a new build job for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		/// <param name="pullRequestNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task TriggerJobRestart(int pullRequestNumber, CancellationToken cancellationToken);
    }
}
