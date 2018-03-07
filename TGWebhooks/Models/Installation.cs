using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents a <see cref="Octokit.Installation"/>
	/// </summary>
    sealed class Installation
	{
		/// <summary>
		/// Primary key for the entity
		/// </summary>
		[Key]
		public int ColumnId { get; set; }

		/// <summary>
		/// The <see cref="Octokit.Installation.Id"/>
		/// </summary>
		public long InstallationId { get; set; }

		/// <summary>
		/// The oauth access token for the <see cref="Installation"/>
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// When <see cref="AccessToken"/> expires
		/// </summary>
		public DateTimeOffset AccessTokenExpiry { get; set; }

		/// <summary>
		/// The <see cref="InstallationRepository"/>s in the <see cref="Installation"/>
		/// </summary>
		public List<InstallationRepository> Repositories { get; set; }
    }
}
