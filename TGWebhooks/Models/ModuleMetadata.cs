using System;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Map of <see cref="Modules.IModule.Uid"/> to its configured enabled status
	/// </summary>
    sealed class ModuleMetadata
    {
		/// <summary>
		/// The <see cref="Modules.IModule.Uid"/>
		/// </summary>
		public Guid Id { get; set; }
		/// <summary>
		/// <see langword="true"/> if the <see cref="Modules.IModule"/> is enabled, <see langword="false"/> otherwise
		/// </summary>
		public bool Enabled { get; set; }
    }
}
