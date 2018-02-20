using System;
using System.Collections.Generic;

namespace TGWebhooks.Configuration
{
#pragma warning disable CA1812
	sealed class PluginConfiguration
#pragma warning restore CA1812
	{
		public Dictionary<Guid, bool> EnabledPlugins { get; set; } = new Dictionary<Guid, bool>();
    }
}
