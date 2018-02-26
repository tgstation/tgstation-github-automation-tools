using System;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Update for a <see cref="Modules.IModule"/>'s enabled status
	/// </summary>
    public sealed class ModuleUpdate
    {
		/// <summary>
		/// The <see cref="Modules.IModule.Uid"/>
		/// </summary>
		public string Uid { get; set; }

		/// <summary>
		/// The new enabled status for the <see cref="Modules.IModule"/>
		/// </summary>
		public bool Enabled { get; set; }
    }
}
