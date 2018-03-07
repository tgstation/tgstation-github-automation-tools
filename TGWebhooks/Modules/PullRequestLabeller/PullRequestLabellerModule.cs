using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Models;

namespace TGWebhooks.Modules.PullRequestLabeller
{
	/// <summary>
	/// <see cref="IModule"/> for auto labelling a <see cref="PullRequest"/>
	/// </summary>
	public sealed class PullRequestLabellerModule : IModule, IPayloadHandler<PullRequestEventPayload>, IMergeRequirement
	{

		/// <inheritdoc />
		public Guid Uid => new Guid("3a6dd37c-3dee-4a7a-a016-885a4a775968");

		/// <inheritdoc />
		public string Name => stringLocalizer["Name"];

		/// <inheritdoc />
		public string Description => stringLocalizer["Description"];

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public string RequirementDescription => stringLocalizer["RequirementDescription"];

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="PullRequestLabellerModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="PullRequestLabellerModule"/>
		/// </summary>
		readonly IStringLocalizer<PullRequestLabellerModule> stringLocalizer;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Construct a <see cref="PullRequestLabellerModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public PullRequestLabellerModule(IGitHubManager gitHubManager, IStringLocalizer<PullRequestLabellerModule> stringLocalizer)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <summary>
		/// Label a pull request
		/// </summary>
		/// <param name="payload">The <see cref="PullRequestEventPayload"/> for the pull request</param>
		/// <param name="oneCheckTags"><see langword="true"/> if additional tags should be contionally applied, <see langword="false"/> otherwise</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TagPR(PullRequestEventPayload payload, bool oneCheckTags, CancellationToken cancellationToken)
		{
			async Task<bool?> MergeableCheck()
			{
				//check if the PR is mergeable, if not, don't tag it
				bool? mergeable = payload.PullRequest.Mergeable;
				for (var I = 0; !mergeable.HasValue && I < 3; ++I)
				{
					await Task.Delay(I * 1000).ConfigureAwait(false);
					mergeable = (await gitHubManager.GetPullRequest(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, cancellationToken).ConfigureAwait(false)).Mergeable;
				}
				return mergeable;
			};

			var mergeableTask = MergeableCheck().ConfigureAwait(false);
			var filesChanged = gitHubManager.GetPullRequestChangedFiles(payload.PullRequest, cancellationToken).ConfigureAwait(false);
			var currentLabelsTask = gitHubManager.GetIssueLabels(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, cancellationToken).ConfigureAwait(false);

			var labelsToAdd = new List<string>();
			var labelsToRemove = new List<string>();

			var lowerTitle = payload.PullRequest.Title.ToLower(CultureInfo.CurrentCulture);

			if (lowerTitle.Contains("refactor"))
				labelsToAdd.Add("Refactor");
			if (lowerTitle.Contains("[dnm]"))
				labelsToAdd.Add("Do Not Merge");
			if (lowerTitle.Contains("[wip]"))
				labelsToAdd.Add("Work In Progress");
			if (lowerTitle.Contains("revert"))
				labelsToAdd.Add("Revert");
			if (lowerTitle.Contains("removes"))
				labelsToAdd.Add("Removal");

			var mergeableCheck = await mergeableTask;
			if (mergeableCheck.HasValue)
				if (!mergeableCheck.Value)
					labelsToAdd.Add("Merge Conflict");
				else
					labelsToRemove.Add("Merge Conflict");

			var treeToLabelMappings = new Dictionary<string, string>
			{
				{ "_maps", "Map Edit" },
				{ "tools", "Tools" },
				{ "SQL" , "SQL" },
				{ ".github" , "GitHub" }
			};

			var addOnlyTreeToLabelMappings = new Dictionary<string, string>
			{
				{ "icons", "Sprites" },
				{ "sound", "Sounds" },
				{ "config" , "Config Update" },
				{ "code/controllers/configuration/entries" , "Config Update" },
				{ "tgui", "UI" }
			};

			foreach (var I in await filesChanged)
			{
				foreach (var J in treeToLabelMappings)
					if (I.FileName.StartsWith(J.Key, StringComparison.CurrentCulture))
						labelsToAdd.Add(J.Value);
					else
						labelsToRemove.Add(J.Value);
				if (oneCheckTags)
					foreach (var J in addOnlyTreeToLabelMappings)
						if (I.FileName.StartsWith(J.Key, StringComparison.CurrentCulture))
							labelsToAdd.Add(J.Value);
			}

			void UniqueAdd(string label)
			{
				if (!labelsToAdd.Contains(label))
					labelsToAdd.Add(label);
			}

			//github close syntax (without "close" variants)
			if (Regex.IsMatch(payload.PullRequest.Body, "(?i)(fix|fixes|fixed|resolve|resolves|resolved)\\s*#[1-9][0-9]*"))
				UniqueAdd("Fix");

			//run through the changelog
			var changelog = Models.Changelog.GetChangelog(payload.PullRequest, out bool malformed);
			if(changelog != null)
				foreach(var I in changelog.Changes.Select(x => x.Type))
					switch (I)
					{
						case ChangelogEntryType.Admin:
							UniqueAdd("Administration");
							break;
						case ChangelogEntryType.Balance:
							UniqueAdd("Balance/Rebalance");
							break;
						case ChangelogEntryType.BugFix:
							UniqueAdd("Fix");
							break;
						case ChangelogEntryType.Code_Imp:
							UniqueAdd("Code Improvement");
							break;
						case ChangelogEntryType.Config:
							UniqueAdd("Config Update");
							break;
						case ChangelogEntryType.ImageAdd:
							UniqueAdd("Sprites");
							break;
						case ChangelogEntryType.ImageDel:
							UniqueAdd("Sprites");
							UniqueAdd("Removal");
							break;
						case ChangelogEntryType.Refactor:
							UniqueAdd("Refactor");
							break;
						case ChangelogEntryType.RscAdd:
							UniqueAdd("Feature");
							break;
						case ChangelogEntryType.RscDel:
							UniqueAdd("Removal");
							break;
						case ChangelogEntryType.SoundAdd:
							UniqueAdd("Sounds");
							break;
						case ChangelogEntryType.SoundDel:
							UniqueAdd("Sounds");
							UniqueAdd("Removal");
							break;
						case ChangelogEntryType.SpellCheck:
							UniqueAdd("Grammar and Formatting");
							break;
						case ChangelogEntryType.Tweak:
							UniqueAdd("Tweak");
							break;
					}

			labelsToAdd.RemoveAll(x => labelsToRemove.Contains(x));
			
			var newLabels = new List<string>();
			foreach (var I in labelsToAdd)
				newLabels.Add(I);

			var currentLabels = new List<Label>(await currentLabelsTask);

			currentLabels.RemoveAll(x => labelsToRemove.Contains(x.Name) || labelsToAdd.Contains(x.Name));
			foreach (var I in currentLabels)
				newLabels.Add(I.Name);

			await gitHubManager.SetIssueLabels(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, newLabels, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (gitHubManager == null)
				throw new InvalidOperationException("Configure() wasn't called!");
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			switch (payload.Action)
			{
				case "opened":
					await TagPR(payload, true, cancellationToken).ConfigureAwait(false);
					return;
				case "synchronize":
					await TagPR(payload, false, cancellationToken).ConfigureAwait(false);
					return;
				case "closed":
					if (payload.PullRequest.Merged)
						break;
					goto default;
				default:
					throw new NotSupportedException();
			}

			Task AddMergeConflictTag(PullRequest pullRequest) => gitHubManager.AddLabel(pullRequest.Base.Repository.Id, pullRequest.Number, "Merge Conflict", cancellationToken);

			async Task RefreshPR(PullRequest pullRequest)
			{
				//wait 10s for refresh then give up
				await Task.Delay(10000).ConfigureAwait(false);
				pullRequest = await gitHubManager.GetPullRequest(pullRequest.Base.Repository.Id, pullRequest.Number, cancellationToken).ConfigureAwait(false);
				if (pullRequest.Mergeable.HasValue && !pullRequest.Mergeable.Value)
					await AddMergeConflictTag(pullRequest).ConfigureAwait(false);
			};

			var tasks = new List<Task>();

			var prs = await gitHubManager.GetOpenPullRequests(payload.PullRequest.Base.Repository.Owner.Login, payload.PullRequest.Base.Repository.Name, cancellationToken).ConfigureAwait(false);
			foreach (var I in prs)
				if (I.Mergeable.HasValue)
				{
					if (I.Mergeable.Value)
						continue;
					else
						tasks.Add(AddMergeConflictTag(I));
				}
				else
					tasks.Add(RefreshPR(I));

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			var labels = await gitHubManager.GetIssueLabels(pullRequest.Base.Repository.Id, pullRequest.Number, cancellationToken).ConfigureAwait(false);

			var result = new AutoMergeStatus
			{
				RequiredProgress = 2,
				Progress = 2
			};

			foreach (var I in labels)
			{
				void CheckLabelDeny(string label) {
					if (I.Name == label)
					{
						--result.Progress;
						result.Notes.Add(stringLocalizer["LabelDeny", label]);
					}
				};
				CheckLabelDeny("Work In Progress");
				CheckLabelDeny("Do Not Merge");
			}
			return result;
		}
	}
}
