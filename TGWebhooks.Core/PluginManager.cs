using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Core.Model;
using TGWebhooks.Api;
using Microsoft.Extensions.Logging;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class PluginManager : IPluginManager
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => plugins.Where(x => x.Enabled).SelectMany(x => x.MergeRequirements).ToList();

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
		/// The <see cref="IRootDataStore"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IRootDataStore rootDataStore;

		/// <summary>
		/// List of loaded <see cref="IPlugin"/>s
		/// </summary>
		IReadOnlyList<IPlugin> plugins;

		/// <summary>
		/// Construct a <see cref="PluginManager"/>
		/// </summary>
		/// <param name="_loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_repository">The value of <see cref="repository"/></param>
		/// <param name="_gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="_requestManager">The value of <see cref="requestManager"/></param>
		/// <param name="_rootDataStore">The value of <see cref="rootDataStore"/></param>
		public PluginManager(ILoggerFactory _loggerFactory, ILogger<PluginManager> _logger, IRepository _repository, IGitHubManager _gitHubManager, IIOManager _ioManager, IWebRequestManager _requestManager, IRootDataStore _rootDataStore)
		{
			loggerFactory = _loggerFactory ?? throw new ArgumentNullException(nameof(_loggerFactory));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			ioManager = _ioManager ?? throw new ArgumentNullException(nameof(_ioManager));
			requestManager = _requestManager ?? throw new ArgumentNullException(nameof(_requestManager));
			rootDataStore = _rootDataStore ?? throw new ArgumentNullException(nameof(_rootDataStore));
		}
		
		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			var logger = loggerFactory.CreateLogger<PluginManager>();

			async Task<PluginConfiguration> DBInit()
			{
				await rootDataStore.Initialize(cancellationToken).ConfigureAwait(false);
				return await rootDataStore.ReadData<PluginConfiguration>("PluginMetaData", cancellationToken).ConfigureAwait(false);
			}
			//start initializing the db
			var dbInit = DBInit();

			var pluginsBuilder = new List<IPlugin>();
			plugins = pluginsBuilder;

			var pluginConfigs = await dbInit.ConfigureAwait(false);
			var dataIOManager = new ResolvingIOManager(ioManager, Application.DataDirectory);

			var tasks = new List<Task<IPlugin>>();

			async Task<IPlugin> InitPlugin(Type type)
			{
				IPlugin plugin;
				try
				{
					logger.LogDebug("Instantiating plugin {0}...", type);
					plugin = (IPlugin)Activator.CreateInstance(type);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to instantiate plugin {0}!", type);
					return null;
				}

				logger.LogTrace("Plugin {0}.Name: {1}", type, plugin.Name);
				logger.LogTrace("Plugin {0}.Description: {1}", type, plugin.Description);
				logger.LogTrace("Plugin {0}.Uid: {1}", type, plugin.Uid);

				var pluginData = rootDataStore.BranchOnKey(plugin.Uid.ToString());
				try
				{
					plugin.Configure(loggerFactory.CreateLogger(type.FullName), repository, gitHubManager, dataIOManager, requestManager, pluginData);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to configure plugin {0}!", type);
					return null;
				}

				if (!pluginConfigs.EnabledPlugins.TryGetValue(plugin.Uid, out bool enabled))
				{
					enabled = true;
					pluginConfigs.EnabledPlugins.Add(plugin.Uid, enabled);
				}

				plugin.Enabled = enabled;
				logger.LogTrace("Plugin {0}.Enabled: {1}", type, enabled);
				if (!enabled)
					return plugin;

				logger.LogDebug("Initializing plugin {0}...", type);
				try
				{
					await plugin.Initialize(cancellationToken).ConfigureAwait(false);
					logger.LogDebug("Plugin {0} initialized!", type);
					return plugin;
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to instantiate plugin {0}!", type);
					return null;
				}
			};
			using (logger.BeginScope("Loading plugins..."))
			{
				var loadedPlugins = await Task.WhenAll(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(y => y.IsPublic && y.IsClass && !y.IsAbstract && typeof(IPlugin).IsAssignableFrom(y) && y.GetConstructors().Any(z => z.IsPublic && z.GetParameters().Count() == 0)).Select(p => InitPlugin(p)))).ConfigureAwait(false);
				pluginsBuilder.AddRange(loadedPlugins.Where(x => x != null));
			}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			logger.LogTrace("Enumerating payload handlers.");
			return plugins.Where(x => x.Enabled).SelectMany(x => x.GetPayloadHandlers<TPayload>());
		}
	}
}
