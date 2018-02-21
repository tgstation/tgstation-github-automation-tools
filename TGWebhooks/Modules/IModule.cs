using System;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Representation of a plugin for <see cref="TGWebhooks"/>
	/// </summary>
	public interface IModule : IComponentProvider
	{
		/// <summary>
		/// A unique <see cref="Uid"/> for the <see cref="IModule"/>
		/// </summary>
		Guid Uid { get; }

		/// <summary>
		/// The name of the <see cref="IModule"/>
		/// </summary>
		string Name { get; }

		/// <summary>
		/// A verbose user-friendly description of the <see cref="IModule"/>
		/// </summary>
		string Description { get; }
	}
}
