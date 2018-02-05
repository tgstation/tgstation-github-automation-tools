using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages the automatic merge process for <see cref="PullRequest"/>s
	/// </summary>
#pragma warning disable CA1812
	sealed class AutoMergeHandler : IPayloadHandler<PullRequestEventPayload>
#pragma warning restore CA1812
	{
		/// <summary>
		/// The <see cref="IComponentProvider"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IComponentProvider componentProvider;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Construct an <see cref="AutoMergeHandler"/>
		/// </summary>
		/// <param name="_componentProvider">The value of <see cref="componentProvider"/></param>
		/// <param name="_gitHubManager">The valuse of <see cref="gitHubManager"/></param>
		public AutoMergeHandler(IComponentProvider _componentProvider, IGitHubManager _gitHubManager)
		{
			componentProvider = _componentProvider ?? throw new ArgumentNullException(nameof(_componentProvider));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
		}

		async Task<IReadOnlyList<AutoMergeStatus>> GetStatusesForPullRequest(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			for (var I = 0; I < 4 && pullRequest.Mergeable == null; ++I)
			{
				await Task.Delay(I * 1000).ConfigureAwait(false);
				pullRequest = await gitHubManager.GetPullRequest(pullRequest.Number).ConfigureAwait(false);
			}

			var tasks = new List<Task<AutoMergeStatus>>();
			foreach (var I in componentProvider.MergeRequirements)
				tasks.Add(I.EvaluateFor(pullRequest, cancellationToken));

			await Task.WhenAll(tasks).ConfigureAwait(false);

			return tasks.Select(x => x.Result).ToList();
		}

		async Task CheckMergePullRequest(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var results = await GetStatusesForPullRequest(pullRequest, cancellationToken).ConfigureAwait(false);

			bool merge = true;
			int rescheduleIn = 0;
			foreach(var I in results)
			{
				if (I.Progress < I.RequiredProgress)
					merge = false;
				if (I.ReevaluateIn > 0)
				{
					if (rescheduleIn == 0)
						rescheduleIn = I.ReevaluateIn;
					else
						rescheduleIn = Math.Min(rescheduleIn, I.ReevaluateIn);
				}
			}

			if (merge)
				await gitHubManager.MergePullRequest(pullRequest).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			
			await CheckMergePullRequest(payload.PullRequest, cancellationToken).ConfigureAwait(false);
		}
	}
}
