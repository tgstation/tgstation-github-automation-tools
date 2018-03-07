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
		/// Schedule a recheck of a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to recheck</param>
		void RecheckPullRequest(PullRequest pullRequest);

		/// <summary>
		/// Invoke the active <see cref="IPayloadHandler{TPayload}"/> for a given <typeparamref name="TPayload"/>
		/// </summary>
		/// <typeparam name="TPayload">The payload type to invoke</typeparam>
		/// <param name="json">The JSON <see cref="string"/> of the <typeparamref name="TPayload"/> to process</param>
		void InvokeHandlers<TPayload>(string json) where TPayload : ActivityPayload;
	}
}
