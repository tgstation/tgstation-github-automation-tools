using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Contains extensions for <see cref="IServiceCollection"/>
	/// </summary>
    static class ServiceCollectionExtensions
    {
		/// <summary>
		/// Add all <see cref="IModule"/> implementations in the <see cref="Assembly.GetExecutingAssembly"/> to a given <paramref name="serviceCollection"/> as singletons
		/// </summary>
		/// <param name="serviceCollection">The <see cref="IServiceCollection"/> to operate on</param>
		/// <returns><paramref name="serviceCollection"/></returns>
		public static IServiceCollection AddModules(this IServiceCollection serviceCollection)
		{
			var moduleType = typeof(IModule);
			var moduleImplementations = Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && moduleType.IsAssignableFrom(x) && !x.IsAbstract).ToList();
			foreach (var I in moduleImplementations)
				serviceCollection.AddSingleton(I);
			serviceCollection.AddSingleton(x => x.GetServices<IModule>());
			return serviceCollection;
		}
    }
}
