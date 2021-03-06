﻿namespace TGWebhooks.Configuration
{
	/// <summary>
	/// GitHub configuration settings
	/// </summary>
    public sealed class GitHubConfiguration
    {
		/// <summary>
		/// The configuration section the <see cref="GitHubConfiguration"/> resides in
		/// </summary>
		public const string Section = "GitHub";

		/// <summary>
		/// The GitHub personal access token used for pushing and API operations
		/// </summary>
		public string PersonalAccessToken { get; set; }

		/// <summary>
		/// The secret to use for hashing webhook payloads
		/// </summary>
		public string WebhookSecret { get; set; }

		/// <summary>
		/// The owner of the repository to manage
		/// </summary>
		public string RepoOwner { get; set; }

		/// <summary>
		/// The name of the repository to manage
		/// </summary>
		public string RepoName { get; set; }

		/// <summary>
		/// The client ID for the Oauth application
		/// </summary>
		public string OauthClientID { get; set; }

		/// <summary>
		/// The client secret for the Oauth application
		/// </summary>
		public string OauthSecret { get; set; }
	}
}
