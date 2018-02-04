using Hangfire;
using Hangfire.SQLite;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Startup point for the web application
	/// </summary>
    sealed class Application
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
			services.AddHangfire(_ => _.UseSQLiteStorage(Path.Combine(DataDirectory, "HangfireDatabase.sqlite3")));
            services.AddMvc();
			services.AddOptions();
			services.Configure<GitHubConfiguration>(configuration.GetSection(GitHubConfiguration.Section));
			services.Configure<TravisConfiguration>(configuration.GetSection(TravisConfiguration.Section));
			services.AddSingleton<IComponentProvider, PluginManager>();
			services.AddSingleton<IGitHubManager, GitHubManager>();
			services.AddSingleton<IRepository, Repository>();
			services.AddSingleton<ILogger, Logger>();
			services.AddSingleton<IIOManager, DefaultIOManager>();
			services.AddSingleton<IWebRequestManager, WebRequestManager>();
			services.AddSingleton<IContinuousIntegration, TravisContinuousIntegration>();
        }

		/// <summary>
		/// Configure the <see cref="Application"/>
		/// </summary>
		/// <param name="app">The <see cref="IApplicationBuilder"/> to configure</param>
		/// <param name="env">The <see cref="IHostingEnvironment"/> to configure</param>
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

			app.UseHangfireServer();
            app.UseMvc();
        }
    }
}
