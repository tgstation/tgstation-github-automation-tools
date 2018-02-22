using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
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

		public ChangelogGeneratorModule(IDataStoreFactory<ChangelogGeneratorModule> dataStoreFactory, IStringLocalizer<ChangelogGeneratorModule> stringLocalizer, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
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
		public Task<string> ModifyMerge(PullRequest pullRequest, string workingCommit, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
