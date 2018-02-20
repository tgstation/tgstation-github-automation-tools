namespace TGWebhooks.Configuration
{
	/// <summary>
	/// Configuration for Travis CI
	/// </summary>
#pragma warning disable CA1812
	sealed class TravisConfiguration
#pragma warning restore CA1812
	{
		/// <summary>
		/// The configuration section the <see cref="TravisConfiguration"/> resides in
		/// </summary>
		public const string Section = "Travis";

		/// <summary>
		/// The access token for using the API
		/// </summary>
		public string APIToken { get; set; }
    }
}
