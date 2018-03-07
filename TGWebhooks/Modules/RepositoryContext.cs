using System;
using System.Collections.Generic;

namespace TGWebhooks.Modules
{
	/// <inheritdoc />
	sealed class RepositoryContext : IRepositoryContext
	{
		/// <inheritdoc />
		public IEnumerable<IModule> EnabledModules => enabledModules;
		/// <inheritdoc />
		public long RepositoryId => repositoryId;

		/// <summary>
		/// The <see cref="SemaphoreSlimContext"/> for the <see cref="RepositoryContext"/>
		/// </summary>
		readonly SemaphoreSlimContext semaphoreContext;
		/// <summary>
		/// Backing field for <see cref="EnabledModules"/>
		/// </summary>
		readonly IEnumerable<IModule> enabledModules;
		/// <summary>
		/// Backing field for <see cref="RepositoryId"/>
		/// </summary>
		readonly long repositoryId;
		/// <summary>
		/// Optional <see cref="Action"/> to run on <see cref="Dispose"/>
		/// </summary>
		readonly Action onDispose;

		/// <summary>
		/// Construct a <see cref="RepositoryContext"/>
		/// </summary>
		/// <param name="semaphoreContext">The value of <see cref="semaphoreContext"/></param>
		/// <param name="enabledModules">The value of <see cref="enabledModules"/></param>
		/// <param name="repositoryId">The value of <see cref="repositoryId"/></param>
		/// <param name="onDispose">The value of <see cref="onDispose"/></param>
		public RepositoryContext(SemaphoreSlimContext semaphoreContext, IEnumerable<IModule> enabledModules, long repositoryId, Action onDispose)
		{
			this.semaphoreContext = semaphoreContext ?? throw new ArgumentNullException(nameof(semaphoreContext));
			this.enabledModules = enabledModules ?? throw new ArgumentNullException(nameof(enabledModules));
			this.repositoryId = repositoryId;
			this.onDispose = onDispose;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			onDispose?.Invoke();
			semaphoreContext.Dispose();
		}
	}
}
