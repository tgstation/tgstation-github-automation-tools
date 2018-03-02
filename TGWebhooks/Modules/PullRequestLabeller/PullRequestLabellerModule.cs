using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Models;

namespace TGWebhooks.Modules.PRTagger
{
	/// <summary>
	/// <see cref="IModule"/> for auto labelling a <see cref="PullRequest"/>
	/// </summary>
	public sealed class PullRequestLabellerModule : IModule, IPayloadHandler<PullRequestEventPayload>
	{

		/// <inheritdoc />
		public Guid Uid => new Guid("3a6dd37c-3dee-4a7a-a016-885a4a775968");

		/// <inheritdoc />
		public string Name => "Pull Request Tagger";

		/// <inheritdoc />
		public string Description => "Automatically labels pull requests based on certain criteria";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="PullRequestLabellerModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Construct a <see cref="PullRequestLabellerModule"/>
		/// </summary>
		/// <param name="gitHubManager">The valus of <see cref="gitHubManager"/></param>
		public PullRequestLabellerModule(IGitHubManager gitHubManager)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
		}

		/// <summary>
		/// Label a pull request
		/// </summary>
		/// <param name="payload">The <see cref="PullRequestEventPayload"/> for the pull request</param>
		/// <param name="oneCheckTags"><see langword="true"/> if additional tags should be contionally applied, <see langword="false"/> otherwise</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task TagPR(PullRequestEventPayload payload, bool oneCheckTags)
		{
			async Task<bool?> MergeableCheck()
			{
				//check if the PR is mergeable, if not, don't tag it
				bool? mergeable = payload.PullRequest.Mergeable;
				for (var I = 0; !mergeable.HasValue && I < 3; ++I)
				{
					await Task.Delay(I * 1000).ConfigureAwait(false);
					mergeable = (await gitHubManager.GetPullRequest(payload.PullRequest.Number).ConfigureAwait(false)).Mergeable;
				}
				return mergeable;
			};

			var mergeableTask = MergeableCheck().ConfigureAwait(false);
			var filesChanged = gitHubManager.GetPullRequestChangedFiles(payload.PullRequest.Number).ConfigureAwait(false);
			var currentLabelsTask = gitHubManager.GetIssueLabels(payload.PullRequest.Number).ConfigureAwait(false);

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

			await gitHubManager.SetIssueLabels(payload.PullRequest.Number, newLabels).ConfigureAwait(false);
		}

		/// <summary>
		/// Checks all open PRs for if they should have the 'Merge Conflict' tag
		/// </summary>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task CheckMergeConflicts()
		{
			Task AddMergeConflictTag(PullRequest pullRequest)
			{
				return gitHubManager.AddLabel(pullRequest.Number, "Merge Conflict");
			};

			async Task RefreshPR(PullRequest pullRequest)
			{
				//wait 10s for refresh then give up
				await Task.Delay(10000).ConfigureAwait(false);
				pullRequest = await gitHubManager.GetPullRequest(pullRequest.Number).ConfigureAwait(false);
				if(pullRequest.Mergeable.HasValue && !pullRequest.Mergeable.Value)
					await AddMergeConflictTag(pullRequest).ConfigureAwait(false);
			};

			var tasks = new List<Task>();

			var prs = await gitHubManager.GetOpenPullRequests().ConfigureAwait(false);
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
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (gitHubManager == null)
				throw new InvalidOperationException("Configure() wasn't called!");
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			switch (payload.Action)
			{
				case "opened":
					return TagPR(payload, true);
				case "synchronize":
					return TagPR(payload, false);
				case "closed":
					if (payload.PullRequest.Merged)
						return CheckMergeConflicts();
					break;
			}
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;
	}
}
