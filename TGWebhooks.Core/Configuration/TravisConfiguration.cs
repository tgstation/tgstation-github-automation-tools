namespace TGWebhooks.Core.Configuration
{
	/// <summary>
	/// Configuration for Travis CI
	/// </summary>
    sealed class TravisConfiguration
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
