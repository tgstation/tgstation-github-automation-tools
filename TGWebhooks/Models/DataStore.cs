using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;

namespace TGWebhooks.Models
{
	/// <inheritdoc />
	sealed class DataStore<TOwnerType> : IDataStore<TOwnerType>
	{
		readonly string prefix;
		readonly IDatabaseContext databaseContext;

		public DataStore(IDatabaseContext databaseContext)
		{
			prefix = typeof(TOwnerType).FullName;
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
	}
}