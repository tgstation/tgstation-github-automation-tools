using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TGWebhooks.Models
{
    sealed class InstallationRepository
	{
		[Key, Column(Order = 0)]
		public long Id { get; set; }
		[Key, Column(Order = 1)]
		public string Slug { get; set; }
	}
}
