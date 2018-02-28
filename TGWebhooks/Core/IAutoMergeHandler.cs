using Octokit;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages the automatic merge process for <see cref="PullRequest"/>s
	/// </summary>
	public interface IAutoMergeHandler : IPayloadHandler<PullRequestEventPayload>
	{
		/// <summary>
		/// Schedule a recheck of a given <paramref name="prNumber"/>
		/// </summary>
		/// <param name="prNumber">The <see cref="PullRequest.Number"/> to recheck</param>
		void RecheckPullRequest(int prNumber);
    }
}
