using Octokit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
		/// The <see cref="IMergeRequirement"/>s the <see cref="IPlugin"/> contains. Will not be accessed until <see cref="Configure(ILogger, IRepository, IGitHubManager)"/> is called
		/// </summary>
		IEnumerable<IMergeRequirement> MergeRequirements { get; }

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
		/// <param name="ioManager">The <see cref="IIOManager"/> for the <see cref="IPlugin"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Configure(ILogger logger, IRepository repository, IGitHubManager gitHub, IIOManager ioManager, CancellationToken cancellationToken);
	}
}
