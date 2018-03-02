using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class AutoMergeHandler : IAutoMergeHandler, IDisposable
#pragma warning restore CA1812
	{
		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="AutoMergeHandler"/>. Necessary because these operations can run without scope, so we must make our own
		/// </summary>
		readonly IServiceProvider serviceProvider;
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly ILogger<AutoMergeHandler> logger;
		/// <summary>
		/// The <see cref="IStringLocalizer{T}"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IStringLocalizer<AutoMergeHandler> stringLocalizer;
		/// <summary>
		/// The <see cref="IBackgroundJobClient"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IBackgroundJobClient backgroundJobClient;
		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// Used for pull request checking serialization
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Construct an <see cref="AutoMergeHandler"/>
		/// </summary>
		/// <param name="_componentProviderFactory">The value of <see cref="componentProviderFactory"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="_backgroundJobClient">The value of <see cref="backgroundJobClient"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		public AutoMergeHandler(IServiceProvider _serviceProvider, ILogger<AutoMergeHandler> _logger,  IStringLocalizer<AutoMergeHandler> _stringLocalizer, IBackgroundJobClient _backgroundJobClient, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			stringLocalizer = _stringLocalizer ?? throw new ArgumentNullException(nameof(_stringLocalizer));
			backgroundJobClient = _backgroundJobClient ?? throw new ArgumentNullException(nameof(_backgroundJobClient));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <summary>
		/// Schedules a recheck of the <see cref="AutoMergeStatus"/>es of a given <paramref name="prNumber"/>
		/// </summary>
		/// <param name="prNumber">The <see cref="PullRequest.Number"/> <see cref="PullRequest"/> to check</param>
		public void RecheckPullRequest(int prNumber) => backgroundJobClient.Enqueue(() => RecheckPullRequest(prNumber, JobCancellationToken.Null));

		/// <summary>
		/// Rechecks the <see cref="AutoMergeStatus"/>es of a given <paramref name="pullRequestNumber"/>
		/// </summary>
		/// <param name="pullRequestNumber">The <see cref="PullRequest.Number"/> <see cref="PullRequest"/> to check</param>
		/// <param name="jobCancellationToken">The <see cref="IJobCancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		[AutomaticRetry(Attempts = 0)]
		public Task RecheckPullRequest(int pullRequestNumber, IJobCancellationToken jobCancellationToken) => CheckMergePullRequest(pullRequestNumber, jobCancellationToken.ShutdownToken);

		/// <summary>
		/// Checks if a given <paramref name="pullRequest"/> is considered mergeable and does so if need be and sets it's commit status
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CheckMergePullRequest(int prNumber, CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			using (logger.BeginScope("Checking mergability of pull request #{0}.", prNumber))
			using (serviceProvider.CreateScope())
			{
				var componentProvider = serviceProvider.GetRequiredService<IComponentProvider>();
				var gitHubManager = serviceProvider.GetRequiredService<IGitHubManager>();
				var continuousIntegration = serviceProvider.GetRequiredService<IContinuousIntegration>();
				var pullRequest = await gitHubManager.GetPullRequest(prNumber).ConfigureAwait(false);
				if (pullRequest.State.Value == ItemState.Closed)
				{
					logger.LogDebug("Pull request is closed!");
					return;
				}
				bool merge = true;
				string mergerToken = null;
				int rescheduleIn = 0;
				Task pendingStatusTask = null;
				try
				{
					pendingStatusTask = gitHubManager.SetCommitStatus(pullRequest, CommitState.Pending, stringLocalizer["CommitStatusPending"]);
					for (var I = 1; I < 4 && !pullRequest.Mergeable.HasValue; ++I)
					{
						await Task.Delay(I * 1000, cancellationToken).ConfigureAwait(false);
						logger.LogTrace("Rechecking git mergeablility.");
						pullRequest = await gitHubManager.GetPullRequest(pullRequest.Number).ConfigureAwait(false);
					}

					if (!pullRequest.Mergeable.HasValue || !pullRequest.Mergeable.Value)
					{
						logger.LogDebug("Aborted due to lack of mergeablility!");
						return;
					}
					await componentProvider.Initialize(cancellationToken).ConfigureAwait(false);

					var tasks = new List<Task<AutoMergeStatus>>();
					foreach (var I in componentProvider.MergeRequirements)
						tasks.Add(I.EvaluateFor(pullRequest, cancellationToken));

					await Task.WhenAll(tasks).ConfigureAwait(false);

					bool goodStatus = true;
					var failReasons = new List<string>();
					foreach (var I in tasks.Select(x => x.Result))
					{
						if (I.Progress < I.RequiredProgress)
						{
							logger.LogDebug("Aborting merge due to status failure: {0}/{1}", I.Progress, I.RequiredProgress);
							if (merge)
								merge = false;
							if (I.FailStatusReport)
							{
								goodStatus = false;
								failReasons.AddRange(I.Notes);
							}
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

					var failReasonMessage = String.Empty;
					foreach (var I in failReasons)
						failReasonMessage = String.Format(CultureInfo.InvariantCulture, "{0}{1}  - {2}", failReasonMessage, Environment.NewLine, I);

					await pendingStatusTask.ConfigureAwait(false);
					await gitHubManager.SetCommitStatus(pullRequest, goodStatus ? CommitState.Success : CommitState.Failure, goodStatus ? stringLocalizer["CommitStatusSuccess"] : stringLocalizer["CommitStatusFail", failReasonMessage]).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					if (pendingStatusTask.Exception != null && pendingStatusTask.Exception != e)
						logger.LogError(e, "Error setting pending status!");
					logger.LogDebug(e, "Error occurred. Setting commit state to errored.");
					try
					{
						await gitHubManager.SetCommitStatus(pullRequest, CommitState.Error, stringLocalizer["CommitStatusError", e]).ConfigureAwait(false);
					}
					catch (Exception e2)
					{
						logger.LogError(e2, "Unable to create error status!");
					}
					throw;
				}

				if (merge)
				{
					//check CI now
					var ciStatus = await continuousIntegration.GetJobStatus(pullRequest, cancellationToken).ConfigureAwait(false);

					switch (ciStatus)
					{
						case ContinuousIntegrationStatus.Failed:
							//lets just fuck off then
							return;
						case ContinuousIntegrationStatus.Passed:
							break;
						case ContinuousIntegrationStatus.Errored:
						case ContinuousIntegrationStatus.PassedOutdated:
							await continuousIntegration.TriggerJobRestart(pullRequest, cancellationToken).ConfigureAwait(false);
							goto case ContinuousIntegrationStatus.NotPresent;
						case ContinuousIntegrationStatus.NotPresent:
						case ContinuousIntegrationStatus.Pending:
							backgroundJobClient.Schedule(() => RecheckPullRequest(pullRequest.Number, JobCancellationToken.Null), DateTimeOffset.UtcNow.AddMinutes(generalConfiguration.CIRecheckInterval));
							return;
					}

					if (mergerToken == null)
						logger.LogWarning("Not merging due to lack of provided merger token!");
					else
					{
						await gitHubManager.MergePullRequest(pullRequest, mergerToken, pullRequest.Head.Sha).ConfigureAwait(false);
						return;
					}
				}

				if (rescheduleIn > 0)
				{
					var targetTime = DateTimeOffset.UtcNow.AddSeconds(rescheduleIn);
					BackgroundJob.Schedule(() => RecheckPullRequest(pullRequest.Number, JobCancellationToken.Null), targetTime);
					logger.LogDebug("Pull request recheck scheduled for {0}.", targetTime);
				}
				else
					logger.LogTrace("Not rescheduling pull request check.");
			}
		}

		/// <inheritdoc />
		public Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));

			return CheckMergePullRequest(payload.PullRequest.Number, cancellationToken);
		}
	}
}
