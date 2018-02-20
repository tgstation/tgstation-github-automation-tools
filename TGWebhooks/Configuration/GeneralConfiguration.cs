using System;

namespace TGWebhooks.Configuration
{
	/// <summary>
	/// General <see cref="Core.Application"/> settings
	/// </summary>
	public sealed class GeneralConfiguration
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
		/// The URL used to access the <see cref="Core.Application"/>
		/// </summary>
		public Uri RootURL { get; set; }
    }
}
