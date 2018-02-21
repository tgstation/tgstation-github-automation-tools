using System.Collections.Generic;

namespace TGWebhooks.Configuration
{
	/// <summary>
	/// Configuration for communicating with BYOND servers
	/// </summary>
    sealed class ServerConfiguration
	{
		/// <summary>
		/// The section <see cref="ServerEntry"/>s reside in
		/// </summary>
		public const string Section = "Servers";

		/// <summary>
		/// The value of <see cref="Byond.TopicSender.IByondTopicSender.SendTimeout"/>
		/// </summary>
		public int SendTimeout { get; set; }

		/// <summary>
		/// The value of <see cref="Byond.TopicSender.IByondTopicSender.ReceiveTimeout"/>
		/// </summary>
		public int ReceiveTimeout { get; set; }

		/// <summary>
		/// <see cref="List{T}"/> of <see cref="ServerEntry"/>s
		/// </summary>
		public List<ServerEntry> Entries { get; set; } = new List<ServerEntry>();
	}
}
