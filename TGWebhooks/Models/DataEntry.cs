using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Simple string KV store
	/// </summary>
    sealed class DataEntry
    {
		/// <summary>
		/// The <see cref="Modules.IModule.Uid"/> the <see cref="DataEntry"/> belongs to
		/// </summary>
		[Key, Column(Order = 0)]
		public Guid ModuleUid { get; set; }

		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/> the <see cref="DataEntry"/> belongs to
		/// </summary>
		[Key, Column(Order = 1)]
		public long RepositoryId { get; set; }

		/// <summary>
		/// The data key
		/// </summary>
		[Key, Column(Order = 2)]
		public string Key { get; set; }

		/// <summary>
		/// The value of the data
		/// </summary>
		public string Value { get; set; }
    }
}
