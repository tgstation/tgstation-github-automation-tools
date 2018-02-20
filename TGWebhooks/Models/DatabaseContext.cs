using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;

namespace TGWebhooks.Models
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class DatabaseContext : DbContext, IDatabaseContext
#pragma warning restore CA1812
	{
		/// <inheridoc />
		public DbSet<AccessTokenEntry> AccessTokenEntries { get; set; }
		/// <inheridoc />
		public DbSet<KeyValuePair> KeyValuePairs { get; set; }
		/// <inheridoc />
		public DbSet<ModuleMetadata> ModuleMetadatas { get; set; }

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseConfiguration> databaseConfigurationOptions) : base(options)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
		}

		/// <inheridoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			switch(databaseConfiguration.DatabaseType.ToUpperInvariant())
			{
				case "SQLSERVER":
				case "MSSQL":
					optionsBuilder.UseSqlServer(databaseConfiguration.ConnectionString);
					break;
				case "MYSQL":
					optionsBuilder.UseMySQL(databaseConfiguration.ConnectionString);
					break;
				case "SQLITE3":
				case "SQLITE":
					optionsBuilder.UseSqlite(databaseConfiguration.ConnectionString);
					break;
				default:
					throw new InvalidOperationException("Invalid database type!");
			}
		}

		/// <inheridoc />
		public Task Save(CancellationToken cancellationToken)
		{
			return SaveChangesAsync(cancellationToken);
		}
	}
}
