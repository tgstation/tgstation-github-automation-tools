using Octokit;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Describes continuous integration status for a <see cref="PullRequest"/>
	/// </summary>
	interface IContinuousIntegration
    {
		/// <summary>
		/// Checks the job status for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ContinuousIntegrationStatus"/> of the job</returns>
		Task<ContinuousIntegrationStatus> GetJobStatus(PullRequest pullRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a new build job for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task TriggerJobRestart(PullRequest pullRequest, CancellationToken cancellationToken);
    }
}
