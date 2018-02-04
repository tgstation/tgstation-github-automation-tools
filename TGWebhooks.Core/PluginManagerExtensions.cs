using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace TGWebhooks.Core
{
    static class PluginManagerExtensions
    {
		public static void UsePluginManager(this IApplicationBuilder app, IApplicationLifetime applicationLifetime)
		{
			var initializationTask = new TaskCompletionSource<object>();
			applicationLifetime.ApplicationStarted.Register(async () =>
			{
				var pluginManager = (IPluginManager)app.ApplicationServices.GetService(typeof(IPluginManager));
				if (pluginManager == null)
					throw new InvalidOperationException("No IPluginManager configured in ApplicationServices!");

				try
				{
					await pluginManager.Initialize(applicationLifetime.ApplicationStopping);
					initializationTask.SetResult(null);
				}
				catch (OperationCanceledException)
				{
					initializationTask.SetCanceled();
				}
			});

			app.Use(async (context, next) =>
			{
				await initializationTask.Task;
				await next.Invoke();
			});
		}
	}
}
