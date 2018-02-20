using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;
using TGWebhooks.Core.Models;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class PluginManager : IPluginManager
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => pluginsAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.MergeRequirements);

		public IEnumerable<IMergeHook> MergeHooks => pluginsAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.MergeHooks);

		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly ILogger<PluginManager> logger;
		/// <summary>
		/// The <see cref="IRepository"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IRepository repository;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="IWebRequestManager"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IWebRequestManager requestManager;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// The <see cref="IStringLocalizerFactory"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IStringLocalizerFactory stringLocalizerFactory;

		/// <summary>
		/// <see cref="IDictionary{TKey, TValue}"/> of loaded <see cref="IModule"/>s and enabled status
		/// </summary>
		IDictionary<IModule, bool> pluginsAndEnabledStatus;

		/// <summary>
		/// Instantiates all <see cref="IModule"/>s
		/// </summary>
		/// <returns>An <see cref="IEnumerable{T}"/> of instantiated <see cref="IModule"/>s</returns>
		static IEnumerable<IModule> InstantiatePlugins()
		{
			var moduleType = typeof(IModule);
			foreach (var I in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && moduleType.IsAssignableFrom(x) && !x.IsAbstract))
				yield return (IModule)Activator.CreateInstance(I);
		}

		/// <summary>
		/// Construct a <see cref="PluginManager"/>
		/// </summary>
		/// <param name="_loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_repository">The value of <see cref="repository"/></param>
		/// <param name="_gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="_requestManager">The value of <see cref="requestManager"/></param>
		/// <param name="_databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="_stringLocalizerFactory">The value of <see cref="stringLocalizerFactory"/></param>
		public PluginManager(ILoggerFactory _loggerFactory, ILogger<PluginManager> _logger, IRepository _repository, IGitHubManager _gitHubManager, IIOManager _ioManager, IWebRequestManager _requestManager, IDatabaseContext _databaseContext, IStringLocalizerFactory _stringLocalizerFactory)
		{
			loggerFactory = _loggerFactory ?? throw new ArgumentNullException(nameof(_loggerFactory));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			ioManager = _ioManager ?? throw new ArgumentNullException(nameof(_ioManager));
			requestManager = _requestManager ?? throw new ArgumentNullException(nameof(_requestManager));
			databaseContext = _databaseContext ?? throw new ArgumentNullException(nameof(_databaseContext));
			stringLocalizerFactory = _stringLocalizerFactory ?? throw new ArgumentNullException(nameof(_stringLocalizerFactory));
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			var logger = loggerFactory.CreateLogger<PluginManager>();

			var test = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

			pluginsAndEnabledStatus = new Dictionary<IModule, bool>();

			var dataIOManager = new ResolvingIOManager(ioManager, Application.DataDirectory);

			var tasks = new List<Task<IModule>>();

			async Task<KeyValuePair<IModule, bool>> InitPlugin(IModule plugin)
			{
				var type = plugin.GetType();
				logger.LogTrace("Plugin {0}.Name: {1}", type, plugin.Name);
				logger.LogTrace("Plugin {0}.Description: {1}", type, plugin.Description);
				logger.LogTrace("Plugin {0}.Uid: {1}", type, plugin.Uid);

				var pluginData = new DataStore(plugin.Uid, databaseContext);
				try
				{
					plugin.Configure(loggerFactory.CreateLogger(type.FullName), repository, gitHubManager, dataIOManager, requestManager, pluginData, stringLocalizerFactory.Create(type));
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to configure plugin {0}!", type);
					return new KeyValuePair<IModule, bool>(plugin, false);
				}

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
				tasks2.AddRange(InstantiatePlugins().Select(x => InitPlugin(x)));
				await Task.WhenAll(tasks2).ConfigureAwait(false);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
				foreach (var I in tasks2.Select(x => x.Result))
					pluginsAndEnabledStatus.Add(I);
			}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			logger.LogTrace("Enumerating payload handlers.");
			return pluginsAndEnabledStatus.Where(x => x.Value).SelectMany(x => x.Key.GetPayloadHandlers<TPayload>());
		}
	}
}
