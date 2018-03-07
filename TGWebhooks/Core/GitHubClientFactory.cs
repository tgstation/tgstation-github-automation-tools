using GitHubJwt;
using Microsoft.Extensions.Options;
using Octokit;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Models;
using TGWebhooks.Modules;
using System.Collections.Generic;
using System.Globalization;
using Octokit.Internal;
using System.Linq.Expressions;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
	sealed class GitHubClientFactory : IGitHubClientFactory, IPrivateKeySource
	{
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="GitHubClientFactory"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="GitHubClientFactory"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// The <see cref="IWebRequestManager"/> for the <see cref="GitHubClientFactory"/>
		/// </summary>
		readonly IWebRequestManager webRequestManager;

		static GitHubClient CreateBareClient() => new GitHubClient(new ProductHeaderValue(Application.UserAgent));

		/// <summary>
		/// Construct a <see cref="GitHubClientFactory"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="gitHubConfiguration"/></param>
		/// <param name="databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="webRequestManager">The value of <see cref="webRequestManager"/></param>
		public GitHubClientFactory(IOptions<GitHubConfiguration> gitHubConfigurationOptions, IDatabaseContext databaseContext, IWebRequestManager webRequestManager)
		{
			gitHubConfiguration = gitHubConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			this.databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			this.webRequestManager = webRequestManager ?? throw new ArgumentNullException(nameof(webRequestManager));
		}

		/// <inheritdoc />
		public IGitHubClient CreateOauthClient(string accessToken)
		{
			var client = CreateBareClient();
			client.Credentials = new Credentials(accessToken, AuthenticationType.Oauth);
			return client;
		}

		/// <inheritdoc />
		public IGitHubClient CreateAppClient()
		{
			//use app auth
			var jwtFac = new GitHubJwtFactory(this, new GitHubJwtFactoryOptions { AppIntegrationId = gitHubConfiguration.AppID, ExpirationSeconds = 600 });
			var jwt = jwtFac.CreateEncodedJwtToken();
			var client = CreateBareClient();
			client.Credentials = new Credentials(jwt, AuthenticationType.Bearer);
			return client;
		}

		/// <inheritdoc />
		public Task<IGitHubClient> CreateInstallationClient(long repositoryId, CancellationToken cancellationToken) => CreateInstallationClient(x => x.Repositories.Any(y => y.Id == repositoryId), cancellationToken);

		/// <inheritdoc />
		public Task<IGitHubClient> CreateInstallationClient(string owner, string name, CancellationToken cancellationToken)
		{
			var slug = String.Concat(owner, '/', name);
			return CreateInstallationClient(x => x.Repositories.Any(y => y.Slug == slug), cancellationToken);
		}

		/// <summary>
		/// Create a <see cref="IGitHubClient"/> based on a <see cref="Repository.Id"/> in a <see cref="Models.Installation"/>
		/// </summary>
		/// <param name="query">The query to run on <see cref="IDatabaseContext.Installations"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a new <see cref="IGitHubClient"/></returns>
		async Task<IGitHubClient> CreateInstallationClient(Expression<Func<Models.Installation, bool>> query, CancellationToken cancellationToken)
		{
			IReadOnlyList<Octokit.Installation> gitHubInstalls;
			List<Models.Installation> allKnownInstalls;
			IGitHubClient client;
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				var installation = await databaseContext.Installations.Where(query).ToAsyncEnumerable().FirstOrDefault(cancellationToken).ConfigureAwait(false);

				if (installation != default(Models.Installation))
				{
					if (installation.AccessTokenExpiry < DateTimeOffset.UtcNow)
					{
						var newToken = await CreateAppClient().GitHubApps.CreateInstallationToken(installation.InstallationId).ConfigureAwait(false);
						installation.AccessToken = newToken.Token;
						installation.AccessTokenExpiry = newToken.ExpiresAt;
						await databaseContext.Save(cancellationToken).ConfigureAwait(false);
					}
					return CreateOauthClient(installation.AccessToken);
				}

				//do a discovery
				client = CreateAppClient();

				//remove bad installs while we're here
				var allKnownInstallsTask = databaseContext.Installations.ToAsyncEnumerable().ToList();
				gitHubInstalls = await client.GitHubApps.GetAllInstallationsForCurrent().ConfigureAwait(false);
				allKnownInstalls = await allKnownInstallsTask.ConfigureAwait(false);
				databaseContext.Installations.RemoveRange(allKnownInstalls.Where(x => !gitHubInstalls.Any(y => y.Id == x.InstallationId)));
			}

			//add new installs for those that aren't
			var installsToAdd = gitHubInstalls.Where(x => !allKnownInstalls.Any(y => y.InstallationId == x.Id));

			async Task<Models.Installation> CreateAccessToken(Octokit.Installation newInstallation)
			{
				//TODO: Implement this in octokit
				//If you're here and wondering why we're not using pagination, it's because YOU HAVEN'T PORTED THIS TO OCTOKIT YET

				var installationToken = await client.GitHubApps.CreateInstallationToken(newInstallation.Id).ConfigureAwait(false);
				var entity = new Models.Installation
				{
					InstallationId = newInstallation.Id,
					AccessToken = installationToken.Token,
					AccessTokenExpiry = installationToken.ExpiresAt,
					Repositories = new List<InstallationRepository>()
				};

				var json = await webRequestManager.RunRequest(new Uri("https://api.github.com/installation/repositories"), null, new List<string> { "Accept: application/vnd.github.machine-man-preview+json", "User-Agent: " + Application.UserAgent, String.Format(CultureInfo.InvariantCulture, "Authorization: token {0}", installationToken.Token) }, RequestMethod.GET, cancellationToken).ConfigureAwait(false);
				var jsonObj = JObject.Parse(json);
				var array = jsonObj["repositories"];
				var repos = new SimpleJsonSerializer().Deserialize<List<Repository>>(array.ToString());

				entity.Repositories.AddRange(repos.Select(x => new InstallationRepository { Id = x.Id, Slug = String.Concat(x.Owner.Login, '/', x.Name) }));

				return entity;
			}

			var newEntities = await Task.WhenAll(installsToAdd.Select(x => CreateAccessToken(x))).ConfigureAwait(false);

			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				await databaseContext.Installations.AddRangeAsync(newEntities).ConfigureAwait(false);
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
			}
			//its either in newEntities now or it doesn't exist
			return CreateOauthClient(newEntities.First(query.Compile()).AccessToken);
		}

		/// <inheritdoc />
		public TextReader GetPrivateKeyReader() => new StringReader(gitHubConfiguration.PemData);
	}
}
