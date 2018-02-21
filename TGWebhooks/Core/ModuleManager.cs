using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Models;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class ModuleManager : IModuleManager
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => modulesAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.MergeRequirements);

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => modulesAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.MergeHooks);
		
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly ILogger<ModuleManager> logger;
		/// <summary>
		/// The <see cref="IRepository"/> for the <see cref="ModuleManager"/>
		/// </summary>
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// All the <see cref="IModule"/>s for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IEnumerable<IModule> allModules;

		/// <summary>
		/// <see cref="IDictionary{TKey, TValue}"/> of loaded <see cref="IModule"/>s and enabled status
		/// </summary>
		IDictionary<IModule, bool> modulesAndEnabledStatus;

		/// <summary>
		/// Construct a <see cref="ModuleManager"/>
		/// </summary>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="_allModules">The value of <see cref="allModules"/></param>
		public ModuleManager(ILogger<ModuleManager> _logger, IDatabaseContext _databaseContext, IEnumerable<IModule> _allModules)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			databaseContext = _databaseContext ?? throw new ArgumentNullException(nameof(_databaseContext));
			allModules = _allModules ?? throw new ArgumentNullException(nameof(_allModules));
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			await databaseContext.Initialize(cancellationToken).ConfigureAwait(false);

			modulesAndEnabledStatus = new Dictionary<IModule, bool>();

			var tasks = new List<Task<IModule>>();

			async Task<KeyValuePair<IModule, bool>> InitPlugin(IModule plugin)
			{
				var type = plugin.GetType();
				logger.LogTrace("Plugin {0}.Name: {1}", type, plugin.Name);
				logger.LogTrace("Plugin {0}.Description: {1}", type, plugin.Description);
				logger.LogTrace("Plugin {0}.Uid: {1}", type, plugin.Uid);

				var query = databaseContext.ModuleMetadatas.Where(x => x.Id == plugin.Uid);
				var result = await query.ToAsyncEnumerable().FirstOrDefault().ConfigureAwait(false);

				bool enabled;
				Task addInTask = null;
				if (result == default(ModuleMetadata))
				{
					enabled = true;
					addInTask = databaseContext.ModuleMetadatas.AddAsync(new ModuleMetadata { Id = plugin.Uid, Enabled = true });
				}
				else
					enabled = result.Enabled;
				
				logger.LogTrace("Plugin {0}.Enabled: {1}", type, enabled);

				logger.LogDebug("Initializing plugin {0}...", type);
				try
				{
					try
					{
						await plugin.Initialize(cancellationToken).ConfigureAwait(false);
						logger.LogDebug("Plugin {0} initialized!", type);
						return new KeyValuePair<IModule, bool>(plugin, enabled);
					}
					catch (Exception e)
					{
						logger.LogError(e, "Failed to initialize plugin {0}!", type);
						return new KeyValuePair<IModule, bool>(plugin, false);
					}
				}
				finally
				{
					if (addInTask != null)
						await addInTask.ConfigureAwait(false);
				}
			};

			using (logger.BeginScope("Loading plugins..."))
			{
				var tasks2 = new List<Task<KeyValuePair<IModule, bool>>>();
				tasks2.AddRange(allModules.Select(x => InitPlugin(x)));
				await Task.WhenAll(tasks2).ConfigureAwait(false);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
				foreach (var I in tasks2.Select(x => x.Result))
					modulesAndEnabledStatus.Add(I);
			}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			logger.LogTrace("Enumerating payload handlers.");
			return modulesAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.GetPayloadHandlers<TPayload>());
		}
	}
}
