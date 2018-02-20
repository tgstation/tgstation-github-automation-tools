using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

namespace TGWebhooks.Modules.TwentyFourHourRule
{
	/// <summary>
	/// <see cref="IModule"/> for the 24-Hour Rule <see cref="IMergeRequirement"/>
	/// </summary>
	public sealed class TwentyFourHourRuleModule : IModule, IMergeRequirement
	{
		/// <summary>
		/// Twenty. Four. Hour. Rule.
		/// </summary>
		const int HoursRequired = 24;

		/// <inheritdoc />
		public Guid Uid => new Guid("78544889-5447-47f2-b300-3fb7b703c3cc");

		/// <inheritdoc />
		public string Name => "24-Hour Rule";

		/// <inheritdoc />
		public string Description => "Merge requirement of having 24 hours pass since the pull request was originally opened";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <inheritdoc />
		public void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager webRequestManager, IDataStore dataStore, IStringLocalizer stringLocalizer)
		{
			//intentionally left blank
		}

		/// <inheritdoc />
		public Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			var hoursSinceOpened = (int)Math.Floor((new DateTimeOffset(DateTime.UtcNow) - pullRequest.CreatedAt).TotalHours);

			var good = hoursSinceOpened > HoursRequired;

			return Task.FromResult(new AutoMergeStatus
			{
				Progress = hoursSinceOpened,
				RequiredProgress = HoursRequired,
				ReevaluateIn = good ? 0 : (HoursRequired - hoursSinceOpened) * 60 * 60,
			});
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			return Enumerable.Empty<IPayloadHandler<TPayload>>();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
