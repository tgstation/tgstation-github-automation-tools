using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core.Model
{
	/// <summary>
	/// A <see cref="IBranchingDataStore"/> based on <see cref="Dictionary{TKey, TValue}"/>s
	/// </summary>
	sealed class DictionaryDataStore : IBranchingDataStore 
	{
		/// <summary>
		/// The parent <see cref="IDataStore"/>
		/// </summary>
		readonly IBranchingDataStore parent;
		/// <summary>
		/// The key for this <see cref="IDataStore"/> on <see cref="parent"/>
		/// </summary>
		readonly string parentKey;

		/// <summary>
		/// Construct a <see cref="DictionaryDataStore"/>
		/// </summary>
		/// <param name="_parent">The value of <see cref="parent"/></param>
		/// <param name="_parentKey">The value of <see cref="parentKey"/></param>
		public DictionaryDataStore(IBranchingDataStore _parent, string _parentKey)
		{
			parent = _parent ?? throw new ArgumentNullException(nameof(_parent));
			parentKey = _parentKey ?? throw new ArgumentNullException(nameof(_parentKey));
		}

		/// <inheritdoc />
		public IBranchingDataStore BranchOnKey(string key)
		{
			return new DictionaryDataStore(parent, key);
		}

		/// <inheritdoc />
		public async Task<TData> ReadData<TData>(string key, CancellationToken cancellationToken) where TData : class
		{
			var parentResult = await parent.ReadData<IDictionary<string, object>>(parentKey, cancellationToken);
			if (parentResult.TryGetValue(key, out object data))
				return data as TData;
			return default;
		}

		/// <inheritdoc />
		public Task WriteData<TData>(string key, TData data, CancellationToken cancellationToken) where TData : class
		{
			return WriteData(new List<string> { key }, data, cancellationToken);
		}

		public Task WriteData<TData>(IEnumerable<string> keys, TData data, CancellationToken cancellationToken)
		{
			IEnumerable<string> Enumerator()
			{
				yield return parentKey;
				foreach (var I in keys)
					yield return I;
			}
			return parent.WriteData(Enumerator(), data, cancellationToken);
		}
	}
}
