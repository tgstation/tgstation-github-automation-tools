using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
	sealed class PluginManager : IPluginManager
	{
		/// <summary>
		/// Directory relative to the current <see cref="Assembly"/> that contains <see cref="IPlugin"/> <see cref="Assembly"/>s
		/// </summary>
		const string PluginDllsDirectory = "Plugins";

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
		public PluginManager(ILogger _logger, IRepository _repository, IGitHubManager _gitHubManager, IIOManager _ioManager)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
			ioManager = _ioManager ?? throw new ArgumentNullException(nameof(_ioManager));
		}

		/// <summary>
		/// Load <see cref="IPlugin"/>s from <see cref="Assembly"/>s in the <see cref="PluginDllsDirectory"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task LoadAllPlugins(CancellationToken cancellationToken) {
			var assemblyPath = Assembly.GetExecutingAssembly().Location;
			var pluginDirectory = ioManager.ConcatPath(ioManager.GetDirectoryName(assemblyPath), PluginDllsDirectory);

#if DEBUG
			pluginDirectory = @"C:\app\bin\Debug\netstandard2.0\";
#endif

			var pluginsBuilder = new List<IPlugin>();
			plugins = pluginsBuilder;
			
			if (!await ioManager.DirectoryExists(pluginDirectory, cancellationToken))
				return;

			bool CompatibilityPredicate(Type x) => x.IsPublic && x.IsClass && !x.IsAbstract && typeof(IPlugin).IsAssignableFrom(x) && x.GetConstructors().Any(y => y.IsPublic && y.GetParameters().Count() == 0);

			foreach (var I in await ioManager.GetFilesInDirectory(PluginDllsDirectory, ".dll", cancellationToken))
				try
				{
					var assembly = Assembly.ReflectionOnlyLoadFrom(I);
				
					var possiblePlugins = assembly.GetTypes().Where(CompatibilityPredicate);

					if (!possiblePlugins.Any())
						continue;

					Assembly.LoadFrom(I);
				}
				catch(Exception e)
				{
					await logger.LogUnhandledException(e, cancellationToken);
				}

			var dataIOManager = new ResolvingIOManager(ioManager, Application.DataDirectory);

			//load once from the AppDomain to ensure that we don't instance multiple copies of the same type
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(CompatibilityPredicate)))
				try
				{
					var plugin = (IPlugin)Activator.CreateInstance(type);
					await plugin.Configure(logger, repository, gitHubManager, dataIOManager, cancellationToken);
					pluginsBuilder.Add(plugin);
				}
				catch (Exception e)
				{
					await logger.LogUnhandledException(e, cancellationToken);
				}
		}

		/// <inheritdoc />
		public async Task<List<IPayloadHandler<TPayload>>> GetActivePayloadHandlers<TPayload>(CancellationToken cancellationToken) where TPayload : ActivityPayload
		{
			await LoadAllPlugins(cancellationToken);
			return plugins.Where(x => x.Enabled).SelectMany(x => x.GetPayloadHandlers<TPayload>()).ToList();
		}
		
		/// <inheritdoc />
		public async Task<List<IMergeRequirement>> GetActiveMergeRequirements(CancellationToken cancellationToken)
		{
			await LoadAllPlugins(cancellationToken);
			return plugins.Where(x => x.Enabled).SelectMany(x => x.MergeRequirements).ToList();
		}
	}
}
