using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core.Model
{
	sealed class SQLiteDataStore : IRootDataStore, IDisposable
	{
		/// <summary>
		/// File name for the <see cref="SQLiteDataStore"/> in the <see cref="Application.DataDirectory"/>
		/// </summary>
		const string FileName = "datastore.sqlite3";
		/// <summary>
		/// Table name for the <see cref="SQLiteDataStore"/>
		/// </summary>
		const string TableName = "json_data";
		/// <summary>
		/// Primary id column for the <see cref="SQLiteDataStore"/>
		/// </summary>
		const string KeyColumn = "uid";
		/// <summary>
		/// JSON column for the <see cref="SQLiteDataStore"/>
		/// </summary>
		const string JsonColumn = "json";
		/// <summary>
		/// Used for formatting prepared queries on the <see cref="KeyColumn"/>
		/// </summary>
		const string KeyParameter = "key";
		/// <summary>
		/// Used for formatting prepared queries on the <see cref="JsonColumn"/>
		/// </summary>
		const string JsonParameter = "value";

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SQLiteDataStore"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The database connection
		/// </summary>
		readonly SqliteConnection connection;

		/// <summary>
		/// Construct a <see cref="SQLiteDataStore"/>
		/// </summary>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		public SQLiteDataStore(IIOManager _ioManager)
		{
			ioManager = _ioManager ?? throw new ArgumentNullException(nameof(_ioManager));

			var builder = new SqliteConnectionStringBuilder
			{
				DataSource = ioManager.ConcatPath(Application.DataDirectory, FileName),
				Mode = SqliteOpenMode.ReadWriteCreate
			};
			connection = new SqliteConnection(builder.ConnectionString);
		}

		/// <summary>
		/// Dispose the <see cref="connection"/>
		/// </summary>
		public void Dispose()
		{
			connection.Dispose();
		}

		/// <summary>
		/// Get the JObject representing the entry with a given primary <paramref name="key"/>
		/// </summary>
		/// <param name="key">The primary key to lookup</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="JObject"/> at the given primary <paramref name="key"/></returns>
		async Task<JObject> GetRootObject(string key, CancellationToken cancellationToken)
		{
			var getCommand = connection.CreateCommand();
			getCommand.CommandText = String.Format(CultureInfo.InvariantCulture, "SELECT {0} FROM {1} WHERE {2} = @{3}", JsonColumn, TableName, KeyColumn, KeyParameter);
			getCommand.Parameters.Add(new SqliteParameter(KeyParameter, key));
			getCommand.Prepare();
			var json = (string)await getCommand.ExecuteScalarAsync(cancellationToken);
			if (json == null)
				return new JObject();
			return JObject.Parse(json);
		}

		/// <summary>
		/// Writes the given <paramref name="json"/> into the given <paramref name="key"/>
		/// </summary>
		/// <param name="key">The primary key to lookup</param>
		/// <param name="json">The <see cref="JObject"/> to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task WriteRootObject(string key, JObject json, CancellationToken cancellationToken)
		{
			var writeCommand = connection.CreateCommand();
			writeCommand.CommandText = String.Format(CultureInfo.InvariantCulture, "REPLACE INTO TABLE({0}) VALUES ('{1}', @{2}), ('{3}', @{4})", TableName, KeyColumn, KeyParameter, JsonColumn, JsonParameter);
			writeCommand.Parameters.Add(new SqliteParameter(KeyParameter, key));
			writeCommand.Parameters.Add(new SqliteParameter(JsonParameter, json.ToString(Formatting.None)));
			writeCommand.Prepare();
			return writeCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		/// <inheritdoc />
		public async Task Initialize(CancellationToken cancellationToken)
		{
			await connection.OpenAsync(cancellationToken);
			var setupCommand = connection.CreateCommand();
			setupCommand.CommandText = String.Format(CultureInfo.InvariantCulture, "CREATE TABLE IF NOT EXISTS {0} ({1} TEXT, {2} TEXT)", TableName, KeyColumn, JsonColumn);
			await setupCommand.ExecuteNonQueryAsync(cancellationToken);
		}

		/// <inheritdoc />
		public IBranchingDataStore BranchOnKey(string key)
		{
			return new DictionaryDataStore(this, key);
		}

		/// <inheritdoc />
		public async Task<TData> ReadData<TData>(string key, CancellationToken cancellationToken) where TData : class
		{
			return (await GetRootObject(key, cancellationToken)).ToObject<TData>();
		}

		/// <inheritdoc />
		public async Task WriteData<TData>(IEnumerable<string> keys, TData data, CancellationToken cancellationToken)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));

			JObject root = null, current = null, oldCurrent = null;
			string lastKey = null, firstKey = null;
			
			foreach(var I in keys)
			{
				if (root == null)
				{
					root = await GetRootObject(I, cancellationToken);
					current = root;
					firstKey = I;
				}
				else
				{
					if(current == null)
					{
						current = new JObject();
						oldCurrent.Add(new JProperty(lastKey, current));
					}
					lastKey = I;
					oldCurrent = current;
					current = oldCurrent[I] as JObject;
				}
			}

			if (root == null)
				throw new ArgumentException("Enumerable empty!", nameof(keys));

			if (oldCurrent == null)
				//twas the root
				root = new JObject(data);
			else
			{
				if (current != null)
					oldCurrent.Remove(lastKey);
				oldCurrent.Add(new JProperty(lastKey, data));
			}

			await WriteRootObject(firstKey, root, cancellationToken);
		}

		/// <inheritdoc />
		public Task WriteData<TData>(string key, TData data, CancellationToken cancellationToken) where TData : class
		{
			return WriteData(new List<string> { key }, data, cancellationToken);
		}
	}
}
