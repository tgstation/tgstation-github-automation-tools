using Octokit;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages the automatic merge process for <see cref="PullRequest"/>s
	/// </summary>
	public interface IAutoMergeHandler : IPayloadHandler<PullRequestEventPayload>
	{
    }
}
