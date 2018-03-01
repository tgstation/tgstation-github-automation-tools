﻿using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
		/// The <see cref="IRepository"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IRepository repository;
		/// <summary>
		/// The <see cref="IStringLocalizer{T}"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IStringLocalizer<AutoMergeHandler> stringLocalizer;
		/// <summary>
		/// The <see cref="IBackgroundJobClient"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IBackgroundJobClient backgroundJobClient;
		/// <summary>
		/// Used for pull request checking serialization
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Construct an <see cref="AutoMergeHandler"/>
		/// </summary>
		/// <param name="_componentProviderFactory">The value of <see cref="componentProviderFactory"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_repository">The value of <see cref="repository"/></param>
		/// <param name="_stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="_backgroundJobClient">The value of <see cref="backgroundJobClient"/></param>
		public AutoMergeHandler(IServiceProvider _serviceProvider, ILogger<AutoMergeHandler> _logger, IRepository _repository, IStringLocalizer<AutoMergeHandler> _stringLocalizer, IBackgroundJobClient _backgroundJobClient)
		{
			serviceProvider = _serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			stringLocalizer = _stringLocalizer ?? throw new ArgumentNullException(nameof(_stringLocalizer));
			backgroundJobClient = _backgroundJobClient ?? throw new ArgumentNullException(nameof(_backgroundJobClient));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <summary>
		/// Merges a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to merge</param>
		/// <param name="gitHubManager">The <see cref="IGitHubManager"/> to use for merging</param>
		/// <param name="componentProvider">The <see cref="IComponentProvider"/> to use for merging</param>
		/// <param name="mergerToken">The token to use for merging</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task MergePullRequest(PullRequest pullRequest, IGitHubManager gitHubManager, IComponentProvider componentProvider, string mergerToken, CancellationToken cancellationToken)
		{
			var acceptedHEAD = pullRequest.Head.Sha;
			var tokenUserTask = gitHubManager.GetUserLogin(mergerToken, cancellationToken);
			var tokenUser = await tokenUserTask.ConfigureAwait(false);
			var startoffCommit = await repository.CreatePullRequestWorkingCommit(pullRequest, tokenUser, cancellationToken).ConfigureAwait(false);
			//run any merge handlers
			var workingCommit = startoffCommit;
			//component provider already checked by CheckMergePullRequest
			foreach (var I in componentProvider.MergeHooks)
				workingCommit = await I.ModifyMerge(pullRequest, workingCommit, cancellationToken).ConfigureAwait(false);

			var anyUpdate = acceptedHEAD != workingCommit;
			if (anyUpdate)
				//force push this bitch up
				await repository.Push(pullRequest.Head.Repository.HtmlUrl, pullRequest.Head.Ref, workingCommit, tokenUser, mergerToken, true, cancellationToken).ConfigureAwait(false);

			try
			{
				await gitHubManager.MergePullRequest(pullRequest, mergerToken, workingCommit).ConfigureAwait(false);
			}
			catch (Exception e2)
			{
				logger.LogError(e2, "Merge process unable to be completed!");
				try
				{
					if (anyUpdate)
						//change things back to normal for the user's sake
						//no cancellation token due to ciritcal op
						await repository.Push(pullRequest.Head.Repository.HtmlUrl, pullRequest.Head.Ref, acceptedHEAD, tokenUser, mergerToken, true, CancellationToken.None).ConfigureAwait(false);
				}
				catch (Exception e3)
				{
					logger.LogError(e3, "Merge process unable to revert!");
					try
					{
						await gitHubManager.CreateComment(pullRequest.Number, stringLocalizer["MergeProcessInterruption", e3.ToString(), acceptedHEAD]).ConfigureAwait(false);
					}
					catch (Exception e)
					{
						logger.LogError(e, "Unable to comment on merge process failure!");
					}
				}
				throw;
			}
		}

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
		public Task RecheckPullRequest(int pullRequestNumber, IJobCancellationToken jobCancellationToken)
		{
			logger.LogDebug("Running scheduled recheck of pull request #{0}.", pullRequestNumber);
			return CheckMergePullRequest(pullRequestNumber, jobCancellationToken.ShutdownToken);
		}

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
						if (I.Progress < I.RequiredProgress && merge)
						{
							logger.LogDebug("Aborting merge due to status failure: {0}/{1}", I.Progress, I.RequiredProgress);
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
					if (mergerToken == null)
						logger.LogWarning("Not merging due to lack of provided merger token!");
					else
					{
						await MergePullRequest(pullRequest, gitHubManager, componentProvider, mergerToken, cancellationToken).ConfigureAwait(false);
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
