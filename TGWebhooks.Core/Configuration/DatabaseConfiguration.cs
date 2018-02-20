using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TGWebhooks.Core.Configuration
{
	public sealed class DatabaseConfiguration
	{
		public const string Section = "Database";
		public string DatabaseType { get; set; }
		public string ConnectionString { get; set; }
	}
}
