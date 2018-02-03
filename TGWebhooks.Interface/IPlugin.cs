using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Representation of a plugin for <see cref="TGWebhooks"/>
	/// </summary>
    public interface IPlugin : IComponentProvider
	{
		/// <summary>
		/// If the <see cref="IPlugin"/> is enabled or not
		/// </summary>
		bool Enabled { get; set; }

		/// <summary>
		/// The name of the <see cref="IPlugin"/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// A verbose user-friendly description of the <see cref="IPlugin"/>
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Configures the <see cref="IPlugin"/>. Will be called before <see cref="IComponentProvider"/> functionality is used
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="IPlugin"/></param>
		/// <param name="repository">The <see cref="IRepository"/> for the <see cref="IPlugin"/></param>
		/// <param name="gitHubManager">The <see cref="IGitHubManager"/> for the <see cref="IPlugin"/></param>
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="IPlugin"/></param>
		/// <param name="requestManager">The <see cref="IWebRequestManager"/> for the <see cref="IPlugin"/></param>
		void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager requestManager);
	}
}
