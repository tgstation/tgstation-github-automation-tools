using Octokit;
using System;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory
	{
		/// <inheritdoc />
		public IGitHubClient CreateGitHubClient(string accessToken) => new GitHubClient(new ProductHeaderValue(Application.UserAgent)) { Credentials = new Credentials(accessToken ?? throw new ArgumentNullException(nameof(accessToken))) };
	}
}
