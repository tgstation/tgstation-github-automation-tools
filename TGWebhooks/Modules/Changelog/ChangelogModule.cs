using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Octokit;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;

namespace TGWebhooks.Modules.Changelog
{
	/// <summary>
	/// Generates changelog .yaml files
	/// </summary>
	public sealed class ChangelogModule : IModule, IMergeRequirement, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("eb442717-57a2-402f-bfd4-0d4dce80f16a");

		/// <inheritdoc />
		public string Name => stringLocalizer["Name"];

		/// <inheritdoc />
		public string Description => stringLocalizer["Description"];

		/// <inheritdoc />
		public string RequirementDescription => stringLocalizer["RequirementDescription"];

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ChangelogModule"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="ChangelogModule"/>
		/// </summary>
		readonly IDataStore dataStore;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="ChangelogModule"/>
		/// </summary>
		readonly IStringLocalizer<ChangelogModule> stringLocalizer;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="ChangelogModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Construct a <see cref="ChangelogModule"/>
		/// </summary>
		/// <param name="dataStoreFactory">The <see cref="IDataStoreFactory{TModule}"/> to create <see cref="dataStore"/> from</param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		public ChangelogModule(IDataStoreFactory<ChangelogModule> dataStoreFactory, IStringLocalizer<ChangelogModule> stringLocalizer, IOptions<GeneralConfiguration> generalConfigurationOptions, IGitHubManager gitHubManager)
		{
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			dataStore = dataStoreFactory?.CreateDataStore(this) ?? throw new ArgumentNullException(nameof(dataStoreFactory));
		}

		/// <summary>
		/// Gets the <see cref="RequireChangelogEntry"/> for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to check</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="RequireChangelogEntry"/> for the <paramref name="pullRequest"/></returns>
		async Task<RequireChangelogEntry> GetRequired(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			var setRequired = await dataStore.ReadData<RequireChangelogEntry>(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			return setRequired;
		}

		/// <inheritdoc />
		public async Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (viewBag == null)
				throw new ArgumentNullException(nameof(viewBag));

			if (!viewBag.IsMaintainer)
				return;
			
			((IList<string>)viewBag.ModuleViews).Add("/Modules/Changelog/Views/Changelog.cshtml");

			var required = await GetRequired(pullRequest, cancellationToken).ConfigureAwait(false);
			var changelog = Models.Changelog.GetChangelog(pullRequest, out bool malformed);

			viewBag.ChangelogIsRequired = required.Required ?? generalConfiguration.DefaultChangelogRequired;
			viewBag.ChangelogPresent = changelog != null || malformed;
			viewBag.ChangelogMalformed = malformed;

			viewBag.ChangelogRequirementHeader = stringLocalizer["ChangelogRequirementHeader"];
			viewBag.ChangelogRequired = stringLocalizer["ChangelogRequired"];
			viewBag.ChangelogNotRequired = stringLocalizer["ChangelogNotRequired"];
		}

		/// <summary>
		/// Set the <see cref="RequireChangelogEntry"/> for a <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/></param>
		/// <param name="requireChangelogEntry">The <see cref="RequireChangelogEntry"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> representing the running operation</returns>
		public Task SetRequirement(PullRequest pullRequest, RequireChangelogEntry requireChangelogEntry, CancellationToken cancellationToken) => dataStore.WriteData(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, requireChangelogEntry, cancellationToken);

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			var required = await GetRequired(pullRequest, cancellationToken).ConfigureAwait(false);

			var changelog = Models.Changelog.GetChangelog(pullRequest, out bool malformed);

			var result = new AutoMergeStatus
			{
				FailStatusReport = true,
				RequiredProgress = (required.Required.HasValue && required.Required.Value) || malformed ? 1 : 0, //TODO:maintainer_can_modify field
				Progress = changelog != null ? 1 : 0
			};
			if (malformed)
				result.Notes.Add(stringLocalizer["ChangelogMalformed"]);
			else if (required.Required.HasValue)
			{
				if (!required.Required.Value)
					result.Notes.Add(stringLocalizer["NoChangelogRequired"]);
				else
					result.Notes.Add(stringLocalizer["ChangelogRequested"]);
			}
			else if (generalConfiguration.DefaultChangelogRequired)
				result.Notes.Add(stringLocalizer["NeedsChangelog"]);
			return result;
		}
		
		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));

			if (payload.Action != "closed" || !payload.PullRequest.Merged)
				throw new NotSupportedException();

			var changelog = Models.Changelog.GetChangelog(payload.PullRequest, out bool malformed);
			if (changelog == null)
				return;

			var result = new List<Dictionary<string, string>>();
#pragma warning disable CA1308 // Normalize strings to uppercase
			result.AddRange(changelog.Changes.Select(x => new Dictionary<string, string> { { x.Type.ToString().ToLowerInvariant(), x.Text } }));
#pragma warning restore CA1308 // Normalize strings to uppercase

			//create the object graph
			var graph = new
			{
				author = changelog.Author,
				delete_after_temporary_for_replacement = true,
				changes = result
			};
			//hack because '-' isn't a valid identifier in c#
			var yaml = new Serializer().Serialize(graph).Replace("delete_after_temporary_for_replacement", "delete-after", StringComparison.InvariantCulture);

			var title = String.Format(CultureInfo.InvariantCulture, "AutoChangeLog-pr-{0}.yml", payload.PullRequest.Number);

			var pathToWrite = Path.Combine("html", "changelogs", title);

			await gitHubManager.CreateFile(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Base.Ref, stringLocalizer["CommitMessage", payload.PullRequest.Number], pathToWrite, yaml, cancellationToken).ConfigureAwait(false);
		}
	}
}
