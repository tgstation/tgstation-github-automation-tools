using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

namespace TGWebhooks.Core.Model
{
	/// <summary>
	/// <see cref="IDataStore"/> with branching capabilities
	/// </summary>
    interface IBranchingDataStore : IDataStore
	{
		/// <summary>
		/// Create a new branch in the <see cref="IBranchingDataStore"/>
		/// </summary>
		/// <param name="key">The key to branch on</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the branched <see cref="IDataStore"/></returns>
		IBranchingDataStore BranchOnKey(string key);

		/// <summary>
		/// Write some nested data to the <see cref="IBranchingDataStore"/>
		/// </summary>
		/// <typeparam name="TData">The POCO to write</typeparam>
		/// <param name="keys"><see cref="IEnumerable{T}"/> of <see cref="string"/>s representing nested keys</param>
		/// <param name="data">The <typeparamref name="TData"/> to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteParentData<TData>(IEnumerable<string> keys, TData data, CancellationToken cancellationToken);
	}
}
