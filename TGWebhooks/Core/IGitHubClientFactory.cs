using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Factory for <see cref="IGitHubClient"/>s
	/// </summary>
	interface IGitHubClientFactory
    {
		/// <summary>
		/// Create a <see cref="IGitHubClient"/> based on an <paramref name="accessToken"/>
		/// </summary>
		/// <param name="accessToken">Oauth access token</param>
		/// <returns>A new <see cref="IGitHubClient"/></returns>
		IGitHubClient CreateOauthClient(string accessToken);

		/// <summary>
		/// Create a <see cref="GitHubApp"/> level <see cref="IGitHubClient"/>
		/// </summary>
		/// <returns>A new <see cref="IGitHubClient"/> valid for only 60s</returns>
		IGitHubClient CreateAppClient();

		/// <summary>
		/// Create a <see cref="IGitHubClient"/> based on a <see cref="Repository.Id"/> in a <see cref="Models.Installation"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Repository.Id"/> in the <see cref="Models.Installation"/> to use</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IGitHubClient"/></returns>
		Task<IGitHubClient> CreateInstallationClient(long repositoryId, CancellationToken cancellationToken);

		/// <summary>
		/// Create a <see cref="IGitHubClient"/> based on a <see cref="Repository.Id"/> in a <see cref="Models.Installation"/>
		/// </summary>
		/// <param name="owner">The <see cref="Repository.Owner"/> of the <paramref name="name"/>d <see cref="Repository"/> to use</param>
		/// <param name="name">The <see cref="Repository.Name"/> in the <see cref="Models.Installation"/> to user</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IGitHubClient"/></returns>
		Task<IGitHubClient> CreateInstallationClient(string owner, string name, CancellationToken cancellationToken);
	}
}
