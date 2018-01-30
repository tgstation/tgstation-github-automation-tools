using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Entry <see langword="class"/> for the application
	/// </summary>
    static class Program
    {
		/// <summary>
		/// The <see cref="IWebHostBuilder"/> for the <see cref="Program"/>
		/// </summary>
		static readonly IWebHostBuilder webHostBuilder = WebHost.CreateDefaultBuilder();

		/// <summary>
		/// Entry point for the <see cref="Program"/>
		/// </summary>
		/// <param name="args">The command line arguments</param>
		/// <returns>A <see cref="Task"/> representing the scope of the <see cref="Program"/></returns>
        public static Task Main(string[] args)
        {
            return webHostBuilder.UseStartup<Application>().Build().RunAsync();
		}            
    }
}
