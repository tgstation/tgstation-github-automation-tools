using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
		/// List of loaded <see cref="IPlugin"/>s
		/// </summary>
		readonly IReadOnlyList<IPlugin> plugins;

		/// <summary>
		/// Construct a <see cref="PluginManager"/>
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for load errors and configuring <see cref="IPlugin"/>s</param>
		/// <param name="repository">The <see cref="IRepository"/> for configuring <see cref="IPlugin"/>s</param>
		/// <param name="gitHub">The <see cref="IGitHubManager"/> for configuring <see cref="IPlugin"/>s</param>
		public PluginManager(ILogger logger, IRepository repository, IGitHubManager gitHub)
		{
			if(logger == null)
				throw new ArgumentNullException(nameof(logger));
			if (repository == null)
				throw new ArgumentNullException(nameof(repository));
			if (gitHub == null)
				throw new ArgumentNullException(nameof(gitHub));

			var assemblyPath = Assembly.GetExecutingAssembly().Location;
			var pluginDirectory = Path.Combine(Path.GetDirectoryName(assemblyPath), PluginDllsDirectory);

			var pluginsBuilder = new List<IPlugin>();
			plugins = pluginsBuilder;

			var dirInfo = new DirectoryInfo(pluginDirectory);

			if (!dirInfo.Exists)
				return;

			bool CompatibilityPredicate(Type x) => x.IsPublic && x.IsClass && !x.IsAbstract && typeof(IPlugin).IsAssignableFrom(x) && x.GetConstructors().Any(y => y.IsPublic && y.GetParameters().Count() == 0);

			foreach (var I in dirInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly))
				try
				{
					var assembly = Assembly.ReflectionOnlyLoadFrom(I.FullName);
				
					var possiblePlugins = assembly.GetTypes().Where(CompatibilityPredicate);

					if (!possiblePlugins.Any())
						continue;

					Assembly.LoadFrom(I.FullName);
				}
				catch(Exception e)
				{
					logger.LogUnhandledException(e);
				}

			//load once from the AppDomain to ensure that we don't instance multiple copies of the same type
			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes().Where(CompatibilityPredicate)))
				try
				{
					var plugin = (IPlugin)Activator.CreateInstance(type);
					plugin.Configure(logger, repository, gitHub);
					pluginsBuilder.Add(plugin);
				}
				catch (Exception e)
				{
					logger.LogUnhandledException(e);
				}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetActivePayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			foreach (var plugin in plugins)
				if (plugin.Enabled)
					foreach (var handler in plugin.GetPayloadHandlers<TPayload>())
						yield return handler;
		}
	}
}
