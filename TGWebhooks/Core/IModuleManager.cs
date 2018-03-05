using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages webhook plugins
	/// </summary>
	public interface IModuleManager : IComponentProvider
	{
		/// <summary>
		/// All <see cref="IModule"/>s and their enabled status
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in all <see cref="IModule"/>s and their enabled status</returns>
		Task<IDictionary<IModule, bool>> ModuleStatuses(CancellationToken cancellationToken);

		/// <summary>
		/// Enable or disable a <see cref="IModule"/>
		/// </summary>
		/// <param name="uid">The <see cref="IModule.Uid"/></param>
		/// <param name="enabled">The new enabled status for the <see cref="IModule"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SetModuleEnabled(Guid uid, bool enabled, CancellationToken cancellationToken);
	}
}