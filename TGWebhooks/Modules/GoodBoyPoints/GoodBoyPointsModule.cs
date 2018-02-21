using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.GoodBoyPoints
{
	/// <summary>
	/// Implements the Good Boy Points tracking
	/// </summary>
	sealed class GoodBoyPointsModule : IModule, IPayloadHandler<PullRequestEventPayload>, IMergeRequirement
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("a8875569-8807-4a58-adf6-ac5a408c7e16");

		/// <inheritdoc />
		public string Name => "Good Boy Points";

		/// <inheritdoc />
		public string Description => "Tracks user code improvement ratios";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <summary>
		/// Map of labels to point values
		/// </summary>
		static readonly IReadOnlyDictionary<string, int> LabelValues = new Dictionary<string, int>
		{
			{ "Fix", 2 },
			{ "Refactor", 2 },
			{ "CI/Tests", 3 },
			{ "Code Improvement", 1 },
			{ "Grammar and Formatting", 1 },
			{ "Priority: High", 4 },
			{ "Priority: CRITICAL", 5 },
			{ "Logging", 1 },
			{ "Feedback", 1 },
			{ "Performance", 3 },
			{ "Feature", -1 },
			{ "Balance/Rebalance", -1 }
		};

		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="GoodBoyPointsModule"/>
		/// </summary>
		readonly IDataStore<GoodBoyPointsModule> dataStore;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="GoodBoyPointsModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="GoodBoyPointsModule"/>
		/// </summary>
		readonly IStringLocalizer<GoodBoyPointsModule> stringLocalizer;

		/// <summary>
		/// Construct a <see cref="GoodBoyPointsModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="dataStore">The value of <see cref="gitHubManager"/></param>
		/// <param name="stringLocalizer">The value of <see cref="gitHubManager"/></param>
		public GoodBoyPointsModule(IGitHubManager gitHubManager, IDataStore<GoodBoyPointsModule> dataStore, IStringLocalizer<GoodBoyPointsModule> stringLocalizer)
		{
			this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var userGBP = await dataStore.ReadData<GoodBoyPointsEntry>(pullRequest.User.Login, cancellationToken).ConfigureAwait(false);
			var passed = userGBP.Points >= 0;
			var result = new AutoMergeStatus
			{
				FailStatusReport = !passed,
				Progress = userGBP.Points,
				RequiredProgress = 0
			};
			if (!passed)
				result.Notes.Add(stringLocalizer["InsufficientGBP"]);
			return result;
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			if (payload.Action != "closed" || !payload.PullRequest.Merged)
				throw new NotSupportedException();

			var labelsTask = gitHubManager.GetIssueLabels(payload.PullRequest.Number);
			var gbp = await dataStore.ReadData<GoodBoyPointsEntry>(payload.PullRequest.User.Login, cancellationToken).ConfigureAwait(false);

			foreach (var L in await labelsTask.ConfigureAwait(false))
				switch (L.Name)
				{
					case "PRB: No Update":
						return;
					case "PRB: Reset":
						gbp = new GoodBoyPointsEntry();
						goto exitLoop;
					default:
						if (LabelValues.TryGetValue(L.Name, out int award))
							gbp.Points += award;
						break;
				}
			exitLoop:

			await dataStore.WriteData(payload.PullRequest.User.Login, gbp, cancellationToken);
		}
	}
}
