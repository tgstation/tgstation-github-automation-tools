using System;

namespace TGWebhooks.Core.Models
{
    sealed class ModuleMetadata
    {
		public Guid Id { get; set; }

		public bool Enabled { get; set; }
    }
}
