namespace TGWebhooks.Configuration
{
	/// <summary>
	/// Indicates a BYOND server
	/// </summary>
    sealed class ServerConfiguration
    {
		/// <summary>
		/// The section <see cref="ServerConfiguration"/>s reside in
		/// </summary>
		public const string Section = "Servers";
		/// <summary>
		/// The address of the server
		/// </summary>
		public string Address { get; set; }
		/// <summary>
		/// The port of the server
		/// </summary>
		public ushort Port { get; set; }
		/// <summary>
		/// The communication key for the server
		/// </summary>
		public string CommsKey { get; set; }
    }
}
