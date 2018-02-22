using Octokit;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Factory for <see cref="IGitHubClient"/>s
	/// </summary>
    interface IGitHubClientFactory
    {
		/// <summary>
		/// Create a <see cref="IGitHubClient"/>
		/// </summary>
		/// <param name="accessToken">The accessToken to use as credentials for the <see cref="IGitHubClient"/></param>
		/// <returns>A new <see cref="IGitHubClient"/></returns>
		IGitHubClient CreateGitHubClient(string accessToken);
    }
}
