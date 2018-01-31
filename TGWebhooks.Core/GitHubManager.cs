using Octokit;
using System;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
    sealed class GitHubManager : IGitHubManager
    {
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubConfiguration configuration;
		/// <summary>
		/// The <see cref="GitHubClient"/> for the <see cref="GitHubManager"/>
		/// </summary>
		readonly GitHubClient gitHubClient;

		/// <summary>
		/// Construct a <see cref="GitHubManager"/>
		/// </summary>
		/// <param name="_configuration">The value of <see cref="configuration"/></param>
		public GitHubManager(GitHubConfiguration _configuration)
		{
			configuration = _configuration ?? throw new ArgumentNullException(nameof(_configuration));
			gitHubClient = new GitHubClient(new ProductHeaderValue("tgstation-github-automation-tools"))
			{
				Credentials = new Credentials(configuration.PersonalAccessToken)
			};
		}
    }
}
