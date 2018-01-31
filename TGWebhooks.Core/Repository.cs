using LibGit2Sharp;
using System;
using System.IO;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
    sealed class Repository : Interface.IRepository, IDisposable
    {
		/// <summary>
		/// Path to the data directory all repositories are stored in
		/// </summary>
		const string RepositoriesDirectory = "Repositories";

		/// <summary>
		/// The <see cref="LibGit2Sharp.Repository"/> for the <see cref="Repository"/>
		/// </summary>
		readonly LibGit2Sharp.Repository repositoryObject;

		/// <summary>
		/// Construct a <see cref="Repository"/>
		/// </summary>
		/// <param name="gitHubConfiguration">The <see cref="GitHubConfiguration"/> to use for determining the <see cref="repositoryObject"/>'s path</param>
		/// <param name="logger">The <see cref="ILogger"/> to use for setting up the <see cref="Repository"/></param>
		public Repository(GitHubConfiguration gitHubConfiguration, ILogger logger)
		{
			var repoPath = Path.Combine(Application.DataDirectory, RepositoriesDirectory, gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName);

			var valid = LibGit2Sharp.Repository.IsValid(repoPath);
			try
			{
				if (!valid)
					LibGit2Sharp.Repository.Clone(String.Format("https://github.com/{0}/{1}", gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName), repoPath, new CloneOptions
					{
						Checkout = true,
						RecurseSubmodules = true
					});
				repositoryObject = new LibGit2Sharp.Repository(repoPath);
				repositoryObject.RemoveUntrackedFiles();
				repositoryObject.RetrieveStatus();
			}
			catch (Exception e)
			{
				logger.LogUnhandledException(e);
				throw;
			}
		}

		/// <summary>
		/// Disposes the <see cref="repositoryObject"/>
		/// </summary>
		public void Dispose()
		{
			repositoryObject.Dispose();
		}
	}
}
