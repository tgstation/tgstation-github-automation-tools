namespace TGWebhooks.Configuration
{
	/// <summary>
	/// Database configuration settings
	/// </summary>
	public sealed class DiscordConfiguration
	{
		/// <summary>
		/// The configuration section the <see cref="DiscordConfiguration"/> resides in
		/// </summary>
		public const string Section = "Discord";

		/// <summary>
		/// The discord bot token to use
		/// </summary>
		public string BotToken { get; set; }

		/// <summary>
		/// The time to wait for discord requests
		/// </summary>
		public int Timeout { get; set; }
	}
}
