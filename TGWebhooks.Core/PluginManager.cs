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
using TGWebhooks.Core.Configuration;
using TGWebhooks.Core.Model;

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
		/// The <see cref="IRootDataStore"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IRootDataStore rootDataStore;
		/// <summary>
		/// The <see cref="IStringLocalizerFactory"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly IStringLocalizerFactory stringLocalizerFactory;

		/// <summary>
		/// List of loaded <see cref="IPlugin"/>s
		/// </summary>
		IReadOnlyDictionary<IPlugin, bool> pluginsAndEnabledStatus;

		/// <summary>
		/// Instantiates all <see cref="IPlugin"/>s
		/// </summary>
		/// <returns>An <see cref="IEnumerable{T}"/> of instantiated <see cref="IPlugin"/>s</returns>
		static IEnumerable<IPlugin> InstantiatePlugins()
		{
			yield return new Plugins.MaintainerApproval.MaintainerApprovalPlugin();
			yield return new Plugins.PRTagger.PRTaggerPlugin();
			yield return new Plugins.SignOff.SignOffPlugin();
			yield return new Plugins.TwentyFourHourRule.TwentyFourHourRulePlugin();
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
		/// <param name="_rootDataStore">The value of <see cref="rootDataStore"/></param>
		/// <param name="_stringLocalizerFactory">The value of <see cref="stringLocalizerFactory"/></param>
		public PluginManager(ILoggerFactory _loggerFactory, ILogger<PluginManager> _logger, IRepository _repository, IGitHubManager _gitHubManager, IIOManager _ioManager, IWebRequestManager _requestManager, IRootDataStore _rootDataStore, IStringLocalizerFactory _stringLocalizerFactory)
		{
			loggerFactory = _loggerFactory ?? throw new ArgumentNullException(nameof(_loggerFactory));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			ioManager = _ioManager ?? throw new ArgumentNullException(nameof(_ioManager));
			requestManager = _requestManager ?? throw new ArgumentNullException(nameof(_requestManager));
			rootDataStore = _rootDataStore ?? throw new ArgumentNullException(nameof(_rootDataStore));
			stringLocalizerFactory = _stringLocalizerFactory ?? throw new ArgumentNullException(nameof(_stringLocalizerFactory));
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			var logger = loggerFactory.CreateLogger<PluginManager>();

			var test = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

			async Task<PluginConfiguration> DBInit()
			{
				await rootDataStore.Initialize(cancellationToken).ConfigureAwait(false);
				return await rootDataStore.ReadData<PluginConfiguration>("PluginMetaData", cancellationToken).ConfigureAwait(false);
			}
			//start initializing the db
			var dbInit = DBInit();

			var pluginsBuilder = new Dictionary<IPlugin, bool>();
			pluginsAndEnabledStatus = pluginsBuilder;

			var dataIOManager = new ResolvingIOManager(ioManager, Application.DataDirectory);

			var tasks = new List<Task<IPlugin>>();

			var pluginConfigs = await dbInit.ConfigureAwait(false);

			async Task InitPlugin(IPlugin plugin)
			{
				var type = plugin.GetType();
				logger.LogTrace("Plugin {0}.Name: {1}", type, plugin.Name);
				logger.LogTrace("Plugin {0}.Description: {1}", type, plugin.Description);
				logger.LogTrace("Plugin {0}.Uid: {1}", type, plugin.Uid);

				var pluginData = rootDataStore.BranchOnKey(plugin.Uid.ToString());
				try
				{
					plugin.Configure(loggerFactory.CreateLogger(type.FullName), repository, gitHubManager, dataIOManager, requestManager, pluginData, stringLocalizerFactory.Create(type));
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to configure plugin {0}!", type);
					return;
				}

				if (!pluginConfigs.EnabledPlugins.TryGetValue(plugin.Uid, out bool enabled))
				{
					enabled = true;
					pluginConfigs.EnabledPlugins.Add(plugin.Uid, enabled);
				}
				
				logger.LogTrace("Plugin {0}.Enabled: {1}", type, enabled);
				if (!enabled)
					return;

				logger.LogDebug("Initializing plugin {0}...", type);
				try
				{
					await plugin.Initialize(cancellationToken).ConfigureAwait(false);
					logger.LogDebug("Plugin {0} initialized!", type);
				}
				catch (Exception e)
				{
					logger.LogError(e, "Failed to instantiate plugin {0}!", type);
				}
			};
			using (logger.BeginScope("Loading plugins..."))
			{
				var tasks2 = new List<Task>();
				foreach (var p in InstantiatePlugins())
				{
					tasks2.Add(InitPlugin(p));
					pluginsBuilder.Add(p, pluginConfigs.EnabledPlugins[p.Uid]);
				}
				await Task.WhenAll(tasks).ConfigureAwait(false);
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
