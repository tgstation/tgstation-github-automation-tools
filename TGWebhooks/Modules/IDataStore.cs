using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Simple key-value data store
	/// </summary>
	public interface IDataStore
	{
		/// <summary>
		/// Read some <typeparamref name="TData"/> from the <see cref="IDataStore"/>
		/// </summary>
		/// <typeparam name="TData">The POCO data</typeparam>
		/// <param name="key">The data storage key</param>
		/// <param name="repositoryId">The <see cref="Octokit.Repository.Id"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the read <typeparamref name="TData"/> if it exists, <see langword="null"/> otherwise</returns>
		Task<TData> ReadData<TData>(string key, long repositoryId, CancellationToken cancellationToken) where TData : class, new();

		/// <summary>
		/// Save some <typeparamref name="TData"/> to the <see cref="IDataStore"/>
		/// </summary>
		/// <typeparam name="TData">The POCO data</typeparam>
		/// <param name="key">The data storage key</param>
		/// <param name="repositoryId">The <see cref="Octokit.Repository.Id"/> for the operation</param>
		/// <param name="data">The data to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteData<TData>(string key, long repositoryId, TData data, CancellationToken cancellationToken) where TData : class, new();

		/// <summary>
		/// Exports the entire <see cref="IDataStore"/> as a json <see cref="string"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Octokit.Repository.Id"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the object tree of the <see cref="IDataStore"/></returns>
		Task<Dictionary<string, object>> ExportDictionary(long repositoryId, CancellationToken cancellationToken);
	}
}
