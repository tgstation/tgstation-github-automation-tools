using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Core.Configuration;
using TGWebhooks.Api;
using Microsoft.Extensions.Logging;
using Octokit;
using System.Collections.Generic;
using System.Linq;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class Repository : Api.IRepository, IInitializable, IDisposable
#pragma warning restore CA1812
	{
		/// <summary>
		/// Path to the data directory all repositories are stored in
		/// </summary>
		const string RepositoriesDirectory = "Repositories";

		/// <inheritdoc />
		public string Path => repositoryObject.Info.WorkingDirectory;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="Repository"/>
		/// </summary>
		readonly ILogger<Repository> logger;
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Repository"/>
		/// </summary>
		readonly IIOManager ioManager;
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="Repository"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;

		/// <summary>
		/// The <see cref="LibGit2Sharp.Repository"/> for the <see cref="Repository"/>
		/// </summary>
		LibGit2Sharp.Repository repositoryObject;

		/// <summary>
		/// Used for guarding access to the <see cref="Repository"/>
		/// </summary>
		SemaphoreSlim semaphore;

		/// <summary>
		/// Construct a <see cref="Repository"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GitHubConfiguration"/> to use for determining the <see cref="repositoryObject"/>'s path</param>
		/// <param name="_logger">The <see cref="ILogger"/> to use for setting up the <see cref="Repository"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		public Repository(IOptions<GitHubConfiguration> gitHubConfigurationOptions, ILogger<Repository> _logger, IIOManager _ioManager)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			gitHubConfiguration = gitHubConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			ioManager = new ResolvingIOManager(_ioManager ?? throw new ArgumentNullException(nameof(_ioManager)), _ioManager.ConcatPath(Application.DataDirectory, RepositoriesDirectory));
			semaphore = new SemaphoreSlim(1);
		}

		/// <summary>
		/// Disposes the <see cref="repositoryObject"/>
		/// </summary>
		public void Dispose()
		{
			repositoryObject?.Dispose();
			semaphore.Dispose();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(async () =>
			{
				using (logger.BeginScope("Initializing repository..."))
				{
					var repoPath = ioManager.ResolvePath(ioManager.ConcatPath(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName));

					logger.LogTrace("Repo path evaluated to be: {0}", repoPath);

					try
					{
						logger.LogTrace("Creating repository object.");
						cancellationToken.ThrowIfCancellationRequested();
						repositoryObject = new LibGit2Sharp.Repository(repoPath);

						repositoryObject.RemoveUntrackedFiles();

						cancellationToken.ThrowIfCancellationRequested();

						repositoryObject.RetrieveStatus();
					}
					catch (OperationCanceledException e)
					{
						logger.LogDebug(e, "Repository setup cancelled!");
						repositoryObject?.Dispose();
						throw;
					}
					catch (Exception e)
					{
						cancellationToken.ThrowIfCancellationRequested();
						using (logger.BeginScope("Repository fallback initializing..."))
						{
							repositoryObject?.Dispose();
							try
							{
								logger.LogTrace("Checking repository directory exists.");
								if (await ioManager.DirectoryExists(repoPath, cancellationToken).ConfigureAwait(false))
								{
									logger.LogWarning(e, "Failed to load repository! Deleting and cloning...");
									await ioManager.DeleteDirectory(repoPath, cancellationToken).ConfigureAwait(false);
								}
								else
									logger.LogInformation(e, "Cloning repository...");

								LibGit2Sharp.Repository.Clone(String.Format(CultureInfo.InvariantCulture, "https://github.com/{0}/{1}", gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName), repoPath, new CloneOptions
								{
									Checkout = false,
									RecurseSubmodules = true,
									OnProgress = (a) => !cancellationToken.IsCancellationRequested,
									OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
									OnTransferProgress = (a) => !cancellationToken.IsCancellationRequested
								});

								logger.LogInformation("Repo clone completed.");

								repositoryObject = new LibGit2Sharp.Repository(repoPath);
							}
							catch (UserCancelledException e2)
							{
								logger.LogDebug(e2, "Repository setup cancelled!");
								cancellationToken.ThrowIfCancellationRequested();
							}
							catch (Exception e2)
							{
								logger.LogCritical(e2, "Unable to clone repository!");
								throw;
							}
						}
					}
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}

		/// <inheritdoc />
		public Task<string> CreatePullRequestWorkingCommit(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task<SemaphoreSlimContext> LockToCallStack(CancellationToken cancellationToken)
		{
			return SemaphoreSlimContext.Lock(semaphore, cancellationToken);
		}

		/// <inheritdoc />
		public Task<string> CommitChanges(IEnumerable<string> pathsToStage, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Push(string remote, string branch, string commit, string token, bool force, CancellationToken cancellationToken)
		{
			var remoteObject = repositoryObject.Network.Remotes.Where(x => x.Url == remote).First();
			return Task.Factory.StartNew(() =>
			{
				try
				{
					repositoryObject.Network.Push(remoteObject, commit, String.Format(CultureInfo.InvariantCulture, "{0}{1}:{2}", force ? "+" : String.Empty, commit, branch), new PushOptions()
					{
						OnPushTransferProgress = (a, b, c) => !cancellationToken.IsCancellationRequested
					});
				}
				catch (UserCancelledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
		}
	}
}
