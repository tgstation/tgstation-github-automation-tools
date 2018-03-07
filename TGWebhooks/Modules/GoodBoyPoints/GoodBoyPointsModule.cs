using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.GoodBoyPoints
{
	/// <summary>
	/// Implements the Good Boy Points tracking
	/// </summary>
	public sealed class GoodBoyPointsModule : IModule, IPayloadHandler<PullRequestEventPayload>, IMergeRequirement
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("a8875569-8807-4a58-adf6-ac5a408c7e16");

		/// <inheritdoc />
		public string Name => stringLocalizer["Name"];

		/// <inheritdoc />
		public string Description => stringLocalizer["Description"];

		/// <inheritdoc />
		public string RequirementDescription => stringLocalizer["RequirementDescription"];

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

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
		readonly IDataStore dataStore;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="GoodBoyPointsModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="GoodBoyPointsModule"/>
		/// </summary>
		readonly IStringLocalizer<GoodBoyPointsModule> stringLocalizer;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Calculates the change to a <paramref name="goodBoyPointsEntry"/> given a set of <paramref name="labels"/>
		/// </summary>
		/// <param name="goodBoyPointsEntry">The <see cref="GoodBoyPointsEntry"/> to adjust</param>
		/// <param name="offset">The <see cref="GoodBoyPointsOffset"/> to apply to the result</param>
		/// <param name="labels">The <see cref="Label"/>s to make adjustments from</param>
		/// <returns>A new <see cref="GoodBoyPointsEntry"/> based off changing <paramref name="goodBoyPointsEntry"/> with <paramref name="labels"/></returns>
		static GoodBoyPointsEntry AdjustGBP(GoodBoyPointsEntry goodBoyPointsEntry, GoodBoyPointsOffset offset, IEnumerable<Label> labels)
		{
			var result = new GoodBoyPointsEntry { Points = goodBoyPointsEntry.Points };
			foreach (var L in labels)
				switch (L.Name)
				{
					case "PRB: No Update":
						return new GoodBoyPointsEntry { Points = goodBoyPointsEntry.Points };
					case "PRB: Reset":
						return new GoodBoyPointsEntry();
					default:
						if (LabelValues.TryGetValue(L.Name, out int award))
							result.Points += award;
						break;
				}
			result.Points += offset?.Offset ?? 0;
			return result;
		}

		/// <summary>
		/// Set the <paramref name="offset"/> for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="offset">The <see cref="GoodBoyPointsOffset"/> for the <paramref name="pullRequest"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public Task SetOffset(PullRequest pullRequest, GoodBoyPointsOffset offset, CancellationToken cancellationToken) => dataStore.WriteData(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, offset ?? throw new ArgumentNullException(nameof(offset)), cancellationToken);

		/// <summary>
		/// Construct a <see cref="GoodBoyPointsModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="dataStoreFactory">The <see cref="IDataStoreFactory{TModule}"/> for <see cref="dataStore"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public GoodBoyPointsModule(IGitHubManager gitHubManager, IDataStoreFactory<GoodBoyPointsModule> dataStoreFactory, IStringLocalizer<GoodBoyPointsModule> stringLocalizer)
		{
			dataStore = dataStoreFactory?.CreateDataStore(this) ?? throw new ArgumentNullException(nameof(dataStoreFactory));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var labelsTask = gitHubManager.GetIssueLabels(pullRequest.Base.Repository.Id, pullRequest.Number, cancellationToken);
			var offsetTask = dataStore.ReadData<GoodBoyPointsOffset>(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, cancellationToken);
			var userGBP = await dataStore.ReadData<GoodBoyPointsEntry>(pullRequest.User.Login, pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);

			var newGBP = AdjustGBP(userGBP, await offsetTask.ConfigureAwait(false), await labelsTask.ConfigureAwait(false));

			var passed = newGBP.Points >= 0;
			var result = new AutoMergeStatus
			{
				FailStatusReport = true,
				Progress = newGBP.Points,
				RequiredProgress = 0
			};
			if (!passed)
				result.Notes.Add(stringLocalizer["InsufficientGBP"]);

			result.Notes.Add(stringLocalizer["GBPResult", newGBP.Points - userGBP.Points, newGBP.Points]);
			return result;
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			if (payload.Action != "closed" || !payload.PullRequest.Merged)
				throw new NotSupportedException();

			var labelsTask = gitHubManager.GetIssueLabels(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, cancellationToken);
			var gbpTask = dataStore.ReadData<GoodBoyPointsEntry>(payload.PullRequest.User.Login, payload.PullRequest.Base.Repository.Id, cancellationToken);
			var offset = await dataStore.ReadData<GoodBoyPointsOffset>(payload.PullRequest.Number.ToString(CultureInfo.InvariantCulture), payload.PullRequest.Base.Repository.Id,  cancellationToken).ConfigureAwait(false);

			var gbp = await gbpTask.ConfigureAwait(false);

			gbp = AdjustGBP(gbp, offset, await labelsTask.ConfigureAwait(false));

			await dataStore.WriteData(payload.PullRequest.User.Login, payload.PullRequest.Base.Repository.Id, gbp, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// List all <see cref="GoodBoyPointsEntry"/>s
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// A <see cref="Task{TResult}"/> resulting in the <see cref="Dictionary{TKey, TValue}"/> of good boy points
		public async Task<Dictionary<string, int>> GoodBoyPointsEntries(long repositoryId, CancellationToken cancellationToken)
		{
			var rawDic = await dataStore.ExportDictionary(repositoryId, cancellationToken).ConfigureAwait(false);
			var realDic = new Dictionary<string, int>();
			foreach (var I in rawDic)
				realDic.Add(I.Key, ((JObject)I.Value).ToObject<GoodBoyPointsEntry>().Points);
			return realDic;
		}

		/// <inheritdoc />
		public async Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if(viewBag == null)
				throw new ArgumentNullException(nameof(viewBag));

			((IList<string>)viewBag.ModuleViews).Add("/Modules/GoodBoyPoints/Views/GoodBoyPoints.cshtml");
			viewBag.GBPHeader = stringLocalizer["GBPHeader"];
			viewBag.GBPBaseLabel = stringLocalizer["GBPBaseLabel"];
			viewBag.GBPLabelsLabel = stringLocalizer["GBPLabelsLabel"];
			viewBag.GBPOffsetLabel = stringLocalizer["GBPOffsetLabel"];
			viewBag.AdjustGBPHeader = stringLocalizer["AdjustGBPHeader"];

			var gbpTask = dataStore.ReadData<GoodBoyPointsEntry>(pullRequest.User.Login, pullRequest.Base.Repository.Id, cancellationToken);
			var offset = await dataStore.ReadData<GoodBoyPointsOffset>(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			var gbp = await gbpTask.ConfigureAwait(false);

			viewBag.GBPBase = gbp.Points;
			viewBag.GBPLabels = AdjustGBP(gbp, null, await gitHubManager.GetIssueLabels(pullRequest.Base.Repository.Id, pullRequest.Number, cancellationToken).ConfigureAwait(false)).Points - gbp.Points;
			viewBag.GBPOffset = offset.Offset;
		}

		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;
	}
}
