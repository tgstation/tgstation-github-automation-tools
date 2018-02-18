﻿namespace TGWebhooks.Core.Configuration
{
	/// <summary>
	/// General <see cref="Application"/> settings
	/// </summary>
    sealed class GeneralConfiguration
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
