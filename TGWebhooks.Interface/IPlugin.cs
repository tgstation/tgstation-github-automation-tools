using Octokit;
using System.Collections.Generic;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Representation of a plugin for <see cref="TGWebhooks"/>
	/// </summary>
    public interface IPlugin
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
		/// The <see cref="IPayloadHandler{TPayload}"/>s the plugin contains. Will not be accessed until <see cref="Configure(ILogger, IRepository, IGitHubManager)"/> is called
		/// </summary>
		IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload;

		/// <summary>
		/// Configures the <see cref="IPlugin"/>
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="IPlugin"/></param>
		/// <param name="repository">The <see cref="IRepository"/> for the <see cref="IPlugin"/></param>
		/// <param name="gitHub">The <see cref="IGitHubManager"/> for the <see cref="IPlugin"/></param>
		void Configure(ILogger logger, IRepository repository, IGitHubManager gitHub);
	}
}
