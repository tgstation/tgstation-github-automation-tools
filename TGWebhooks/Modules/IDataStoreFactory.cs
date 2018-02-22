namespace TGWebhooks.Modules
{
	/// <summary>
	/// Factory for creating <see cref="IDataStore"/>s
	/// </summary>
	/// <typeparam name="TModule">The <see cref="IModule"/> the <see cref="IDataStoreFactory{TModule}"/> was made for</typeparam>
    public interface IDataStoreFactory<TModule> where TModule : IModule
    {
		/// <summary>
		/// Create a <see cref="IDataStore"/>
		/// </summary>
		/// <param name="module">The <typeparamref name="TModule"/> that will use the <see cref="IDataStore"/></param>
		/// <returns>A new <see cref="IDataStore"/></returns>
		IDataStore CreateDataStore(TModule module);
    }
}
