using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Modules;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

namespace TGWebhooks.Models
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class DatabaseContext : DbContext, IDatabaseContext
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public DbSet<UserAccessToken> UserAccessTokens
		{
			get
			{
				CheckSemaphoreLocked();
				return userAccessTokens;
			}
			set => userAccessTokens = value;
		}

		/// <inheritdoc />
		public DbSet<DataEntry> DataEntries
		{
			get
			{
				CheckSemaphoreLocked();
				return dataEntries;
			}
			set => dataEntries = value;
		}

		/// <inheritdoc />
		public DbSet<ModuleMetadata> ModuleMetadatas
		{
			get
			{
				CheckSemaphoreLocked();
				return moduleMetadatas;
			}
			set => moduleMetadatas = value;
		}

		/// <inheritdoc />
		public DbSet<Installation> Installations
		{
			get
			{
				CheckSemaphoreLocked();
				return installations;
			}
			set => installations = value;
		}

		/// <inheritdoc />
		public DbSet<InstallationRepository> InstallationRepositories
		{
			get
			{
				CheckSemaphoreLocked();
				return installationRepositories;
			}
			set => installationRepositories = value;
		}

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
		/// The <see cref="SemaphoreSlim"/> for the <see cref="DatabaseContext"/>
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// Backing field for <see cref="UserAccessTokens"/>
		/// </summary>
		DbSet<UserAccessToken> userAccessTokens;
		/// <summary>
		/// Backing field for <see cref="DataEntries"/>
		/// </summary>
		DbSet<DataEntry> dataEntries;
		/// <summary>
		/// Backing field for <see cref="ModuleMetadatas"/>
		/// </summary>
		DbSet<ModuleMetadata> moduleMetadatas;
		/// <summary>
		/// Backing field for <see cref="Installations"/>
		/// </summary>
		DbSet<Installation> installations;
		/// <summary>
		/// Backing field for <see cref="InstallationRepositories"/>
		/// </summary>
		DbSet<InstallationRepository> installationRepositories;

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

		/// <inheritdoc />
		public override void Dispose()
		{
			base.Dispose();
			semaphore.Dispose();
		}

		void CheckSemaphoreLocked()
		{
			if (semaphore.CurrentCount > 0)
				throw new InvalidOperationException("The DatabaseContext is not locked!");
		}

		/// <summary>
		/// Construct a <see cref="DatabaseContext"/>
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="databaseConfiguration"/></param>
		/// <param name="loggerFactory">The value of <see cref="loggerFactory"/></param>
		public DatabaseContext(DbContextOptions<DatabaseContext> options, IOptions<DatabaseConfiguration> databaseConfigurationOptions, ILoggerFactory loggerFactory) : base(options)
		{
			databaseConfiguration = databaseConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(databaseConfigurationOptions));
			this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// build default model.
			LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());

			// real relation database can map table:
			modelBuilder.Entity<Log>().ToTable(nameof(Log));

			//set up multikeys
			modelBuilder.Entity<DataEntry>().HasKey(x => new { x.ModuleUid, x.RepositoryId, x.Key });
			modelBuilder.Entity<ModuleMetadata>().HasKey(x => new { x.Uid, x.RepositoryId });
			modelBuilder.Entity<InstallationRepository>().HasKey(x => new { x.Id, x.Slug });
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			SelectDatabaseType(databaseConfiguration, x => optionsBuilder.UseSqlServer(x), x => optionsBuilder.UseMySQL(x), x => optionsBuilder.UseSqlite(x));
			optionsBuilder.UseLoggerFactory(loggerFactory);
		}

		/// <inheritdoc />
		public Task Save(CancellationToken cancellationToken)
		{
			CheckSemaphoreLocked();
			return SaveChangesAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Database.EnsureCreatedAsync(cancellationToken);


		/// <inheritdoc />
		public Task<SemaphoreSlimContext> LockToCallStack(CancellationToken cancellationToken) => SemaphoreSlimContext.Lock(semaphore, cancellationToken);
	}
}
