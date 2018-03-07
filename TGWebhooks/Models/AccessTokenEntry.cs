using System;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Describes a GitHub AccessToken stored within a cookie
	/// </summary>
	sealed class UserAccessToken
	{
		/// <summary>
		/// The name of the cookie
		/// </summary>
		public const string CookieName = "TG-GAT-GithubAccessToken";

		/// <summary>
		/// The identifier for the cookie
		/// </summary>
		public Guid Id { get; set; }

		/// <summary>
		/// The GitHub access token
		/// </summary>
		public string AccessToken { get; set; }

		/// <summary>
		/// When the represented cookie expires
		/// </summary>
		public DateTimeOffset Expiry { get; set; }
	}
}
