using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Models;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class ModuleManager : IModuleManager, IDisposable
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => modulesAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.MergeRequirements);

		/// <inheritdoc />
		public IDictionary<IModule, bool> ModuleStatuses => modulesAndEnabledStatus;
		
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly ILogger<ModuleManager> logger;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// All the <see cref="IModule"/>s for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IEnumerable<IModule> allModules;

		/// <summary>
		/// Used for initialization
		/// </summary>
		readonly SemaphoreSlim semaphore;

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
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			using (await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false))
			{
				if (modulesAndEnabledStatus != null)
					return;

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
					if (result == default(ModuleMetadata))
					{
						enabled = true;
						await databaseContext.ModuleMetadatas.AddAsync(new ModuleMetadata { Id = plugin.Uid, Enabled = true }).ConfigureAwait(false);
					}
					else
						enabled = result.Enabled;

					logger.LogTrace("Plugin {0}.Enabled: {1}", type, enabled);

					return new KeyValuePair<IModule, bool>(plugin, enabled);
				};

				using (logger.BeginScope("Loading plugins..."))
				{
					var tasks2 = new List<Task<KeyValuePair<IModule, bool>>>();
					tasks2.AddRange(allModules.Select(x => InitPlugin(x)));
					await Task.WhenAll(tasks2).ConfigureAwait(false);
					await databaseContext.Save(cancellationToken).ConfigureAwait(false);
					foreach (var I in tasks2.Select(x => x.Result))
					{
						modulesAndEnabledStatus.Add(I);
						I.Key.SetEnabled(I.Value);
					}
				}
			}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			logger.LogTrace("Enumerating payload handlers.");
			return modulesAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.GetPayloadHandlers<TPayload>());
		}

		/// <inheritdoc />
		public async Task SetModuleEnabled(Guid uid, bool enabled, CancellationToken cancellationToken)
		{
			var module = modulesAndEnabledStatus.Keys.First(x => x.Uid == uid);
			if (modulesAndEnabledStatus[module] == enabled)
			{
				logger.LogInformation("Module {0} already enabled/disabled ({1})", module.Name, enabled);
				return;
			}
			modulesAndEnabledStatus[module] = enabled;
			module.SetEnabled(enabled);

			var dbentry = await databaseContext.ModuleMetadatas.Where(x => x.Id == uid).ToAsyncEnumerable().First(cancellationToken).ConfigureAwait(false);
			dbentry.Enabled = enabled;
			await databaseContext.Save(cancellationToken).ConfigureAwait(false);

			logger.LogInformation("Module {0} enabled status set to {1}", module.Name, enabled);
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken)
		{
			var tasks = new List<Task>();
			foreach (var I in modulesAndEnabledStatus.Where(x => x.Value).Select(x => x.Key))
				//object cast to workaround a runtime binder bug
				tasks.Add(I.AddViewVars(pullRequest, (object)viewBag, cancellationToken));
			return Task.WhenAll(tasks);
		}
		/// <inheritdoc />
		public bool ModuleEnabled<TModule>() where TModule : IModule => modulesAndEnabledStatus.Where(x => x.Key is TModule).Select(x => x.Value).First();
	}
}
