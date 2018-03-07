using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
		[Key, Column(Order = 0)]
		public Guid Uid { get; set; }

		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/>
		/// </summary>
		[Key, Column(Order = 1)]
		public long RepositoryId { get; set; }

		/// <summary>
		/// <see langword="true"/> if the <see cref="Modules.IModule"/> is enabled, <see langword="false"/> otherwise
		/// </summary>
		public bool Enabled { get; set; }
    }
}
