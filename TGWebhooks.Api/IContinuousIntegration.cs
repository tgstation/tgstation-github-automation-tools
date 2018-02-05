using Octokit;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Describes continuous integration status for a <see cref="PullRequest"/>
	/// </summary>
	public interface IContinuousIntegration
    {
		/// <summary>
		/// The name of the <see cref="IContinuousIntegration"/> provider
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Checks the job status for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the <paramref name="pullRequest"/> is from</param>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ContinuousIntegrationStatus"/> of the job</returns>
		Task<ContinuousIntegrationStatus> GetJobStatus(Repository repository, PullRequest pullRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Triggers a new build job for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="repository">The <see cref="Repository"/> the <paramref name="pullRequest"/> is from</param>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task TriggerJobRestart(Repository repository, PullRequest pullRequest, CancellationToken cancellationToken);
    }
}
