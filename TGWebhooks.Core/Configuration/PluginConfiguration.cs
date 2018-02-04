using System;
using System.Collections.Generic;

namespace TGWebhooks.Core.Configuration
{
    sealed class PluginConfiguration
    {
		public Dictionary<Guid, bool> EnabledPlugins { get; set; } = new Dictionary<Guid, bool>();
    }
}
