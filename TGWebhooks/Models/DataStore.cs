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
		readonly Guid moduleUid;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="DataStore"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;

		/// <summary>
		/// Construct a <see cref="DataStore"/>
		/// </summary>
		/// <param name="moduleUid">The value of <see cref="moduleUid"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		public DataStore(Guid moduleUid, IDatabaseContext databaseContext)
		{
			this.moduleUid = moduleUid;
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
		}

		/// <inheritdoc />
		public async Task<TData> ReadData<TData>(string key, long repositoryId, CancellationToken cancellationToken) where TData : class, new()
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));
			var moduleUidString = moduleUid.ToString();
			DataEntry result;
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
				result = await databaseContext.DataEntries.FirstOrDefaultAsync(x => x.ModuleUid == moduleUid && x.RepositoryId == repositoryId && x.Key == key, cancellationToken).ConfigureAwait(false);
			if (result == default(DataEntry))
				return new TData();
			return JsonConvert.DeserializeObject<TData>(result.Value);
		}

		/// <inheritdoc />
		public async Task WriteData<TData>(string key, long repositoryId, TData data, CancellationToken cancellationToken) where TData : class, new()
		{
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			var json = JsonConvert.SerializeObject(data ?? throw new ArgumentNullException(nameof(data)));
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				var result = await databaseContext.DataEntries.FirstOrDefaultAsync(x => x.ModuleUid == moduleUid && x.RepositoryId == repositoryId && x.Key == key, cancellationToken).ConfigureAwait(false);
				if (result == default(DataEntry))
				{
					result = new DataEntry() { ModuleUid = moduleUid, RepositoryId = repositoryId, Key = key };
					databaseContext.DataEntries.Add(result);
				}
				result.Value = json;
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public async Task<Dictionary<string, object>> ExportDictionary(long repositoryId, CancellationToken cancellationToken)
		{
			var results = await databaseContext.DataEntries.Where(x => x.ModuleUid == moduleUid && x.RepositoryId == repositoryId).ToAsyncEnumerable().ToList().ConfigureAwait(false);
			var dic = new Dictionary<string, object>();
			foreach(var I in results)
				dic.Add(I.Key, JsonConvert.DeserializeObject(I.Value));
			return dic;
		}
	}
}