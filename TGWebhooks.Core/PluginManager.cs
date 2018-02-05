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

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class PluginManager : IPluginManager
#pragma warning restore CA1812
	{
		/// <summary>
		/// Directory relative to the current <see cref="Assembly"/> that contains <see cref="IPlugin"/> <see cref="Assembly"/>s
		/// </summary>
		const string PluginDllsDirectory = "Plugins";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => plugins.Where(x => x.Enabled).SelectMany(x => x.MergeRequirements).ToList();

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PluginManager"/>
		/// </summary>
		readonly ILogger logger;
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
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_repository">The value of <see cref="repository"/></param>
		/// <param name="_gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		/// <param name="_requestManager">The value of <see cref="requestManager"/></param>
		/// <param name="_rootDataStore">The value of <see cref="rootDataStore"/></param>
		public PluginManager(ILogger _logger, IRepository _repository, IGitHubManager _gitHubManager, IIOManager _ioManager, IWebRequestManager _requestManager, IRootDataStore _rootDataStore)
		{
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
			async Task<PluginConfiguration> DBInit()
			{
				await rootDataStore.Initialize(cancellationToken).ConfigureAwait(false);
				return await rootDataStore.ReadData<PluginConfiguration>("PluginMetaData", cancellationToken).ConfigureAwait(false);
			}
			//start initializing the db
			var dbInit = DBInit().ConfigureAwait(false);

			var assemblyPath = Assembly.GetExecutingAssembly().Location;
			var pluginDirectory = ioManager.ConcatPath(ioManager.GetDirectoryName(assemblyPath), PluginDllsDirectory);

#if DEBUG
			pluginDirectory = @"C:\app\bin\Debug\netstandard2.0\";
#endif

			var pluginsBuilder = new List<IPlugin>();
			plugins = pluginsBuilder;

			if (!await ioManager.DirectoryExists(pluginDirectory, cancellationToken).ConfigureAwait(false))
			{
				await dbInit;
				return;
			}

			bool CompatibilityPredicate(Type x) => x.IsPublic && x.IsClass && !x.IsAbstract && typeof(IPlugin).IsAssignableFrom(x) && x.GetConstructors().Any(y => y.IsPublic && y.GetParameters().Count() == 0);

			foreach (var I in await ioManager.GetFilesInDirectory(PluginDllsDirectory, ".dll", cancellationToken).ConfigureAwait(false))
				try
				{
					var assembly = Assembly.ReflectionOnlyLoadFrom(I);

					var possiblePlugins = assembly.GetTypes().Where(CompatibilityPredicate);

					if (!possiblePlugins.Any())
						continue;

					Assembly.LoadFrom(I);
				}
				catch (Exception e)
				{
					await logger.LogUnhandledException(e, cancellationToken).ConfigureAwait(false);
				}

			var pluginConfigs = await dbInit;

			var dataIOManager = new ResolvingIOManager(ioManager, Application.DataDirectory);

			var tasks = new List<Task<IPlugin>>();

			async Task<IPlugin> InitPlugin(Type type)
			{
				try
				{
					var plugin = (IPlugin)Activator.CreateInstance(type);
					var pluginData = rootDataStore.BranchOnKey(plugin.Uid.ToString());
					plugin.Configure(logger, repository, gitHubManager, dataIOManager, requestManager, pluginData);
					if (!pluginConfigs.EnabledPlugins.TryGetValue(plugin.Uid, out bool enabled))
					{
						enabled = true;
						pluginConfigs.EnabledPlugins.Add(plugin.Uid, enabled);
					}
					plugin.Enabled = enabled;
					await plugin.Initialize(cancellationToken).ConfigureAwait(false);
					return plugin;
				}
				catch (Exception e)
				{
					await logger.LogUnhandledException(e, cancellationToken).ConfigureAwait(false);
					return null;
				}
			};

			//load once from the AppDomain to ensure that we don't instance multiple copies of the same type
			var loadedPlugins = await Task.WhenAll(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(CompatibilityPredicate)).Select(x => InitPlugin(x))).ConfigureAwait(false);
			pluginsBuilder.AddRange(loadedPlugins.Where(x => x != null));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			return plugins.Where(x => x.Enabled).SelectMany(x => x.GetPayloadHandlers<TPayload>());
		}
	}
}
