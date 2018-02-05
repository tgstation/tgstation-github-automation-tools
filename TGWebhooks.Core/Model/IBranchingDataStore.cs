using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

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
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the branched <see cref="IDataStore"/></returns>
		IBranchingDataStore BranchOnKey(string key);
		Task WriteData<TData>(IEnumerable<string> keys, TData data, CancellationToken cancellationToken);
	}
}
