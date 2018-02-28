using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
		public bool Enabled { get; set; }

		/// <inheritdoc />
		public Guid Uid => new Guid("78544889-5447-47f2-b300-3fb7b703c3cc");

		/// <inheritdoc />
		public string Name => stringLocalizer["Name"];

		/// <inheritdoc />
		public string Description => stringLocalizer["Description"];

		/// <inheritdoc />
		public string RequirementDescription => stringLocalizer["RequirementDescription"];

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload => Enumerable.Empty<IPayloadHandler<TPayload>>();

		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="TwentyFourHourRuleModule"/>
		/// </summary>
		readonly IStringLocalizer<TwentyFourHourRuleModule> stringLocalizer;

		/// <summary>
		/// Construct a <see cref="TwentyFourHourRuleModule"/>
		/// </summary>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public TwentyFourHourRuleModule(IStringLocalizer<TwentyFourHourRuleModule> stringLocalizer)
		{
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
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
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
