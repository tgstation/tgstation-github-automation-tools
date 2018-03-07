using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TGWebhooks.Models
{
    sealed class Installation
	{
		[Key]
		public int ColumnId { get; set; }

		public long InstallationId { get; set; }

		public string AccessToken { get; set; }

		public DateTimeOffset AccessTokenExpiry { get; set; }

		public List<InstallationRepository> Repositories { get; set; }
    }
}
