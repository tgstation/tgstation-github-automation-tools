namespace TGWebhooks.Configuration
{
	/// <summary>
	/// Indicates a BYOND server
	/// </summary>
    sealed class ServerEntry
    {
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
