using System;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Representation of a plugin for <see cref="TGWebhooks"/>
	/// </summary>
	public interface IPlugin : IComponentProvider
	{
		/// <summary>
		/// A unique <see cref="Uid"/> for the <see cref="IPlugin"/>
		/// </summary>
		Guid Uid { get; }

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
		/// <param name="webRequestManager">The <see cref="IWebRequestManager"/> for the <see cref="IPlugin"/></param>
		/// <param name="dataStore">The <see cref="IDataStore"/> for the <see cref="IPlugin"/></param>
		/// <param name="stringLocalizer">The <see cref="IStringLocalizer"/> for the <see cref="IPlugin"/></param>
		void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager webRequestManager, IDataStore dataStore, IStringLocalizer stringLocalizer);
	}
}
