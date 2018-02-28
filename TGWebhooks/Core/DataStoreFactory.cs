using System;
using TGWebhooks.Models;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
	sealed class DataStoreFactory<TModule> : IDataStoreFactory<TModule> where TModule : IModule
	{
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="DataStoreFactory{TModule}"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
	
		/// <summary>
		/// Construct a <see cref="DataStoreFactory{TModule}"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public DataStoreFactory(IDatabaseContext databaseContext)
		{
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		public IDataStore CreateDataStore(TModule ownerModule) => new DataStore(ownerModule?.Uid ?? throw new ArgumentNullException(nameof(ownerModule)), databaseContext);
	}
}
