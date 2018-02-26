using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;

namespace TGWebhooks.Models
{
	/// <inheritdoc />
	sealed class DataStore: IDataStore
	{
		/// <summary>
		/// The <see cref="IModule.Uid"/> for the <see cref="DataStore"/>
		/// </summary>
		readonly Guid prefix;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="DataStore"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// Construct a <see cref="DataStore"/>
		/// </summary>
		/// <param name="prefix">The value of <see cref="prefix"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public DataStore(Guid prefix, IDatabaseContext databaseContext)
		{
			this.prefix = prefix;
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		public async Task<TData> ReadData<TData>(string key, CancellationToken cancellationToken) where TData : class, new()
		{
			key = prefix.ToString() + key ?? throw new ArgumentNullException(nameof(key));
			var result = await databaseContext.KeyValuePairs.FirstOrDefaultAsync(x => x.Key == key, cancellationToken).ConfigureAwait(false);
			if (result == default(KeyValuePair))
				return new TData();
			return JsonConvert.DeserializeObject<TData>(result.Value);
		}

		/// <inheritdoc />
		public async Task WriteData<TData>(string key, TData data, CancellationToken cancellationToken) where TData : class, new()
		{
			key = prefix.ToString() + key ?? throw new ArgumentNullException(nameof(key));
			var json = JsonConvert.SerializeObject(data ?? throw new ArgumentNullException(nameof(data)));
			var result = await databaseContext.KeyValuePairs.FirstOrDefaultAsync(x => x.Key == key, cancellationToken).ConfigureAwait(false);
			if (result == default(KeyValuePair))
			{
				result = new KeyValuePair() { Key = key };
				databaseContext.KeyValuePairs.Add(result);
			}
			result.Value = json;
			await databaseContext.Save(cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task<Dictionary<string, object>> ExportDictionary(CancellationToken cancellationToken)
		{
			var prefixString = prefix.ToString();
			var results = await databaseContext.KeyValuePairs.Where(x => x.Key.StartsWith(prefixString)).ToAsyncEnumerable().ToList().ConfigureAwait(false);
			var dic = new Dictionary<string, object>();
			foreach(var I in results)
				dic.Add(I.Key.Substring(prefixString.Length), JsonConvert.DeserializeObject(I.Value));
			return dic;
		}
	}
}