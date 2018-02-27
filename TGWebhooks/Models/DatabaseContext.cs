using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

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
		/// The <see cref="DbSet{TEntity}"/> for <see cref="Log"/>s
		/// </summary>
		public DbSet<Log> Logs { get; set; }

		/// <summary>
		/// The <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly DatabaseConfiguration databaseConfiguration;
		/// <summary>
		/// The <see cref="ILoggerFactory"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly ILoggerFactory loggerFactory;
		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly IServiceProvider serviceProvider;

		/// <summary>
		/// Helper for calling different <see cref="Action{T}"/>s with <see cref="DatabaseConfiguration.ConnectionString"/> based on <see cref="DatabaseConfiguration.DatabaseType"/>
		/// </summary>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/> to use</param>
		/// <param name="onSQLServer"><see cref="Action{T}"/> if the <see cref="DatabaseConfiguration.DatabaseType"/> is SQL Server</param>
		/// <param name="onMySQL"><see cref="Action{T}"/> if the <see cref="DatabaseConfiguration.DatabaseType"/> is MySQL</param>
		/// <param name="onSQLite"><see cref="Action{T}"/> if the <see cref="DatabaseConfiguration.DatabaseType"/> is SQLite</param>
		public static void SelectDatabaseType(DatabaseConfiguration databaseConfiguration, Action<string> onSQLServer, Action<string> onMySQL, Action<string> onSQLite)
		{
			if (onSQLServer == null)
				throw new ArgumentNullException(nameof(onSQLServer));
			if (onMySQL == null)
				throw new ArgumentNullException(nameof(onMySQL));
			if (onSQLite == null)
				throw new ArgumentNullException(nameof(onSQLite));
			switch (databaseConfiguration?.DatabaseType.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(databaseConfiguration)))
			{
				case "SQLSERVER":
				case "MSSQL":
					onSQLServer(databaseConfiguration.ConnectionString);
					break;
				case "MYSQL":
					onMySQL(databaseConfiguration.ConnectionString);
					break;
				case "SQLITE3":
				case "SQLITE":
					onSQLite(databaseConfiguration.ConnectionString);
					break;
				default:
					throw new InvalidOperationException("Invalid database type!");
			}
		}

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/></param>
		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseConfiguration> databaseConfigurationOptions, ILoggerFactory loggerFactory, IServiceProvider serviceProvider) : base(options)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		/// <inheridoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// build default model.
			LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());

			// real relation database can map table:
			modelBuilder.Entity<Log>().ToTable(nameof(Log));
		}

		/// <inheridoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			SelectDatabaseType(databaseConfiguration, x => optionsBuilder.UseSqlServer(x), x => optionsBuilder.UseMySQL(x), x => optionsBuilder.UseSqlite(x));
			optionsBuilder.UseLoggerFactory(loggerFactory);
		}

		/// <inheridoc />
		public Task Save(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

		/// <inheridoc />
		public Task Initialize(CancellationToken cancellationToken) => Database.EnsureCreatedAsync(cancellationToken);
	}
}
