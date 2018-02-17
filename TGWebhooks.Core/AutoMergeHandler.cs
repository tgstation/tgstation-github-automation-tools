using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TGWebhooks.Api;
using Microsoft.Extensions.Logging;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class AutoMergeHandler : IAutoMergeHandler
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
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly ILogger<AutoMergeHandler> logger;

		/// <summary>
		/// Construct an <see cref="AutoMergeHandler"/>
		/// </summary>
		/// <param name="_componentProvider">The value of <see cref="componentProvider"/></param>
		/// <param name="_gitHubManager">The valuse of <see cref="gitHubManager"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		public AutoMergeHandler(IComponentProvider _componentProvider, IGitHubManager _gitHubManager, ILogger<AutoMergeHandler> _logger)
		{
			componentProvider = _componentProvider ?? throw new ArgumentNullException(nameof(_componentProvider));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
		}

		/// <summary>
		/// Rechecks the <see cref="AutoMergeStatus"/>es of a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check</param>
		/// <param name="jobCancellationToken">The <see cref="IJobCancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task RecheckPullRequest(PullRequest pullRequest, IJobCancellationToken jobCancellationToken)
		{
			logger.LogDebug("Running scheduled recheck of pull request #{0}.", pullRequest.Number);
			try
			{
				pullRequest = await gitHubManager.GetPullRequest(pullRequest.Number).ConfigureAwait(false);
				await CheckMergePullRequest(pullRequest, jobCancellationToken.ShutdownToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException e)
			{
				logger.LogDebug(e, "Pull request recheck cancelled!");
			}
			catch (Exception e)
			{
				logger.LogError(e, "Pull request recheck failed!");
			}
		}

		/// <summary>
		/// Checks if a given <paramref name="pullRequest"/> is considered mergeable and does so if need be
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CheckMergePullRequest(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			using (logger.BeginScope("Checking mergability of pull request #{0}.", pullRequest.Number)) {
				for (var I = 0; I < 4 && !pullRequest.Mergeable.HasValue; ++I)
				{
					await Task.Delay(I * 1000, cancellationToken).ConfigureAwait(false);
					logger.LogTrace("Rechecking git mergeablility.");
					pullRequest = await gitHubManager.GetPullRequest(pullRequest.Number).ConfigureAwait(false);
				}

				if (!pullRequest.Mergeable.HasValue || !pullRequest.Mergeable.Value)
				{
					logger.LogDebug("Aborted due to lack of mergeablility: {0}", pullRequest.Mergeable);
					return;
				}

				var tasks = new List<Task<AutoMergeStatus>>();
				foreach (var I in componentProvider.MergeRequirements)
					tasks.Add(I.EvaluateFor(pullRequest, cancellationToken));

				await Task.WhenAll(tasks).ConfigureAwait(false);

				bool merge = true;
				string mergerToken = null;
				int rescheduleIn = 0;
				foreach (var I in tasks.Select(x => x.Result))
				{
					if (I.Progress < I.RequiredProgress && merge)
					{
						logger.LogDebug("Aborting merge due to status failure: {0}/{1}", I.Progress, I.RequiredProgress);
						merge = false;
					}
					if (I.ReevaluateIn > 0)
					{
						if (rescheduleIn == 0)
							rescheduleIn = I.ReevaluateIn;
						else
							rescheduleIn = Math.Min(rescheduleIn, I.ReevaluateIn);
					}
					if (I.MergerAccessToken != null)
					{
						if (mergerToken != null)
							throw new InvalidOperationException("Multiple AutoMergeResults with MergerAccessTokens!");
						mergerToken = I.MergerAccessToken;
					}
				}

				if (merge)
				{
					if (mergerToken == null)
						logger.LogWarning("Not merging due to lack of provided merger token!");
					else
					{
						await gitHubManager.MergePullRequest(pullRequest, mergerToken).ConfigureAwait(false);
						return;
					}
				}

				if (rescheduleIn > 0)
				{
					var targetTime = DateTimeOffset.UtcNow.AddSeconds(rescheduleIn);
					BackgroundJob.Schedule(() => RecheckPullRequest(pullRequest, JobCancellationToken.Null), targetTime);
					logger.LogDebug("Pull request recheck scheduled for {0}.", targetTime);
				}
				else
					logger.LogTrace("Not rescheduling pull request check.");
			}
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
