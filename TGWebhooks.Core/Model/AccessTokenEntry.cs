using System;

namespace TGWebhooks.Core.Model
{
	/// <summary>
	/// Describes a GitHub AccessToken stored within a cookie
	/// </summary>
    sealed class AccessTokenEntry
    {
		/// <summary>
		/// The name of the cookie
		/// </summary>
		public const string CookieName = "TG-GAT-GithubAccessToken";

		/// <summary>
		/// The identifier for the cookie
		/// </summary>
		public Guid Cookie { get; set; }

		/// <summary>
		/// The GitHub access token
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// When the <see cref="Cookie"/> expires
		/// </summary>
		public DateTimeOffset Expiry { get; set; }
    }
}
