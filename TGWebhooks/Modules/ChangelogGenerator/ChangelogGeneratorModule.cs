using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Octokit;
using SharpYaml;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Models;

namespace TGWebhooks.Modules.ChangelogGenerator
{
	/// <summary>
	/// Generates changelog .yaml files
	/// </summary>
	sealed class ChangelogGeneratorModule : IModule, IMergeRequirement, IMergeHook
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("eb442717-57a2-402f-bfd4-0d4dce80f16a");

		/// <inheritdoc />
		public string Name => "Changelog Generator";

		/// <inheritdoc />
		public string Description => "Generates .yaml changelog files";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => new List<IMergeHook> { this };
		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload => Enumerable.Empty<IPayloadHandler<TPayload>>();

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		readonly IDataStore dataStore;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		readonly IStringLocalizer<ChangelogGeneratorModule> stringLocalizer;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="IRepository"/> for the <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		readonly IRepository repository;

		/// <summary>
		/// Construct a <see cref="ChangelogGeneratorModule"/>
		/// </summary>
		/// <param name="dataStoreFactory">The <see cref="IDataStoreFactory{TModule}"/> to create <see cref="dataStore"/> from</param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/></param>
		public ChangelogGeneratorModule(IDataStoreFactory<ChangelogGeneratorModule> dataStoreFactory, IStringLocalizer<ChangelogGeneratorModule> stringLocalizer, IOptions<GeneralConfiguration> generalConfigurationOptions, IIOManager ioManager, IRepository repository)
		{
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			dataStore = dataStoreFactory?.CreateDataStore(this) ?? throw new ArgumentNullException(nameof(dataStoreFactory));
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));

			var setRequired = await dataStore.ReadData<RequireChangelogEntry>(pullRequest.Number.ToString(), cancellationToken).ConfigureAwait(false);
			
			if (setRequired.Required == null)
				setRequired.Required = generalConfiguration.DefaultChangelogRequired;

			var changelog = Changelog.GetChangelog(pullRequest, out bool malformed);

			var result = new AutoMergeStatus
			{
				FailStatusReport = true,
				RequiredProgress = setRequired.Required.Value || malformed ? 1 : 0,
				Progress = changelog != null ? 1 : 0
			};
			if (result.Progress < result.RequiredProgress || malformed)
				result.Notes.Add(stringLocalizer[malformed ? "ChangelogMalformed" : "NeedsChangelog"]);
			return result;
		}
		
		/// <inheritdoc />
		public async Task<string> ModifyMerge(PullRequest pullRequest, string workingCommit, CancellationToken cancellationToken)
		{
			var changelog = Changelog.GetChangelog(pullRequest, out bool malformed);
			if (changelog == null)
				return workingCommit;
			
			var result = new Dictionary<string, List<string>>();
			foreach (var I in changelog.Changes) {
				var key = I.Type.ToString();
				if (!result.ContainsKey(key))
					result.Add(key, new List<string>());
				result[key].Add(I.Text);
			}

			//create the object graph
			var graph = new
			{
				author = changelog.Author,
				delete_after_temporary_for_replacement = true,
				changes = result
			};
			//hack because '-' isn't a valid identifier in c#
			var yaml = new Serializer().Serialize(graph).Replace("delete_after_temporary_for_replacement", "delete-after");

			var title = String.Format(CultureInfo.InvariantCulture, "AutoChangeLog-pr-{0}.yml", pullRequest.Number);

			var pathToWrite = ioManager.ConcatPath(repository.Path, "html", "changelogs", title);
			await ioManager.WriteAllText(pathToWrite, yaml, cancellationToken).ConfigureAwait(false);

			return await repository.CommitChanges(new List<string> { pathToWrite }, cancellationToken);
		}
	}
}
