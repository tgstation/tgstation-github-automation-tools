namespace TGWebhooks.Core.Configuration
{
	/// <summary>
	/// General <see cref="Application"/> settings
	/// </summary>
#pragma warning disable CA1812
	sealed class GeneralConfiguration
#pragma warning restore CA1812
	{
		/// <summary>
		/// The configuration section the <see cref="GeneralConfiguration"/> resides in
		/// </summary>
		public const string Section = "General";

		/// <summary>
		/// The default locale
		/// </summary>
		public string DefaultLocale { get; set; }

		/// <summary>
		/// The URL used to access the <see cref="Application"/>
		/// </summary>
		public string RootURL { get; set; }
    }
}
