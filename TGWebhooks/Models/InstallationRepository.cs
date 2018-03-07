using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents a <see cref="Octokit.Repository"/> in an <see cref="Installation"/>
	/// </summary>
    sealed class InstallationRepository
	{
		/// <summary>
		/// The <see cref="Octokit.Repository.Id"/>
		/// </summary>
		[Key, Column(Order = 0)]
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Octokit.Repository.Owner"/>/<see cref="Octokit.Repository.Name"/> slug
		/// </summary>
		[Key, Column(Order = 1)]
		public string Slug { get; set; }
	}
}
