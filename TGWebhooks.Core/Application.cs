using Cyberboss.AspNetCore.AsyncInitializer;
using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Api;
using TGWebhooks.Core.Model;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Startup point for the web application
	/// </summary>
#pragma warning disable CA1812
	sealed class Application
#pragma warning restore CA1812
	{
		/// <summary>
		/// The user agent string to provide to various APIs
		/// </summary>
		public const string UserAgent = "tgstation-github-automation-tools";

		/// <summary>
		/// The path to the directory the <see cref="Application"/> should use for data files
		/// </summary>
		public static string DataDirectory { get; private set; }

		/// <summary>
		/// The <see cref="IConfiguration"/> for the <see cref="Application"/>
		/// </summary>
		readonly IConfiguration configuration;

		/// <summary>
		/// Construct an <see cref="Application"/>
		/// </summary>
		/// <param name="_configuration">The value of <see cref="configuration"/></param>
		/// <param name="hostingEnvironment">The <see cref="IHostingEnvironment"/> used to determin the <see cref="DataDirectory"/></param>
		public Application(IConfiguration _configuration, IHostingEnvironment hostingEnvironment)
        {
			if (hostingEnvironment == null)
				throw new ArgumentNullException(nameof(hostingEnvironment));
            configuration = _configuration ?? throw new ArgumentNullException(nameof(_configuration));
			DataDirectory = Path.Combine(hostingEnvironment.ContentRootPath, "App_Data");
		}

		/// <summary>
		/// Configure dependency injected services
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> to configure</param>
        public void ConfigureServices(IServiceCollection services)
        {
			services.AddHangfire(_ => _.UseSQLiteStorage(new SqliteConnectionStringBuilder {
				DataSource = Path.Combine(DataDirectory, "HangfireDatabase.sqlite3"),
				Mode = SqliteOpenMode.ReadWriteCreate
			}.ConnectionString));
            services.AddMvc();
			services.AddOptions();
			services.Configure<GitHubConfiguration>(configuration.GetSection(GitHubConfiguration.Section));
			services.Configure<TravisConfiguration>(configuration.GetSection(TravisConfiguration.Section));
			services.AddSingleton<IPluginManager, PluginManager>();
			services.AddSingleton<IComponentProvider>(x => x.GetRequiredService<IPluginManager>());
			services.AddSingleton<IGitHubManager, GitHubManager>();
			services.AddSingleton<IRepository, Repository>();
			services.AddSingleton<IIOManager, DefaultIOManager>();
			services.AddSingleton<IWebRequestManager, WebRequestManager>();
			services.AddSingleton<IContinuousIntegration, TravisContinuousIntegration>();
			services.AddSingleton<IRootDataStore, SQLiteDataStore>();
			services.AddSingleton<IBranchingDataStore>(x => x.GetRequiredService<IRootDataStore>());
			services.AddSingleton<IAutoMergeHandler, AutoMergeHandler>();
		}

#pragma warning disable CA1822 // Mark members as static
		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="app">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="env">The <see cref="IHostingEnvironment"/> of the <see cref="Application"/></param>
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
#pragma warning restore CA1822 // Mark members as static
		{
			app.ApplicationServices.GetRequiredService<IIOManager>().CreateDirectory(DataDirectory, CancellationToken.None).GetAwaiter().GetResult();

			app.UseAsyncInitialization<IPluginManager>((pluginManager, cancellationToken) => pluginManager.Initialize(cancellationToken));

			if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

			app.UseHangfireServer();
            app.UseMvc();
        }
    }
}
