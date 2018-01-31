namespace TGWebhooks.Core
{
	/// <summary>
	/// GitHub configuration entry
	/// </summary>
    sealed class GitHubConfiguration
    {
		/// <summary>
		/// The GitHub personal access token used for pushing and API operations
		/// </summary>
		public string PersonalAccessToken { get; }

		/// <summary>
		/// The secret to use for hashing webhook payloads
		/// </summary>
		public string WebhookSecret { get; }

		/// <summary>
		/// The owner of the repository to manage
		/// </summary>
		public string RepoOwner { get; }

		/// <summary>
		/// The name of the repository to manage
		/// </summary>
		public string RepoName { get; }
    }
}
