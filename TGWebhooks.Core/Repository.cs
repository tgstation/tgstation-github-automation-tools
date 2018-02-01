using LibGit2Sharp;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
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
		/// The <see cref="ILogger"/> for the <see cref="Repository"/>
		/// </summary>
		readonly ILogger logger;
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
		/// The <see cref="Task"/> associated with creating <see cref="repositoryObject"/>
		/// </summary>
		Task startupTask;

		/// <summary>
		/// Construct a <see cref="Repository"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GitHubConfiguration"/> to use for determining the <see cref="repositoryObject"/>'s path</param>
		/// <param name="_logger">The <see cref="ILogger"/> to use for setting up the <see cref="Repository"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		public Repository(IOptions<GitHubConfiguration> gitHubConfigurationOptions, ILogger _logger, IIOManager _ioManager)
		{
			if (gitHubConfigurationOptions == null)
				throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			ioManager = new ResolvingIOManager(_ioManager ?? throw new ArgumentNullException(nameof(_ioManager)), _ioManager.ConcatPath(Application.DataDirectory, RepositoriesDirectory));
			gitHubConfiguration = gitHubConfigurationOptions.Value;
		}

		/// <summary>
		/// Loads <see cref="repositoryObject"/>, cloning if necessary
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task EnsureAvailable(CancellationToken cancellationToken)
		{
			if (startupTask == null)
				startupTask = Task.Factory.StartNew(async () =>
				{
					var repoPath = ioManager.ResolvePath(ioManager.ConcatPath(gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName));

					try
					{
						repositoryObject = new LibGit2Sharp.Repository(repoPath);
						cancellationToken.ThrowIfCancellationRequested();
						repositoryObject.RemoveUntrackedFiles();
						cancellationToken.ThrowIfCancellationRequested();
						repositoryObject.RetrieveStatus();
					}
					catch (Exception e)
					{
						await logger.LogUnhandledException(e, CancellationToken.None);

						LibGit2Sharp.Repository.Clone(String.Format(CultureInfo.InvariantCulture, "https://github.com/{0}/{1}", gitHubConfiguration.RepoOwner, gitHubConfiguration.RepoName), repoPath, new CloneOptions
						{
							Checkout = true,
							RecurseSubmodules = true,
							OnProgress = (a) => !cancellationToken.IsCancellationRequested,
							OnUpdateTips = (a, b, c) => !cancellationToken.IsCancellationRequested,
							OnTransferProgress = (a) => !cancellationToken.IsCancellationRequested
						});

						cancellationToken.ThrowIfCancellationRequested();

						repositoryObject = new LibGit2Sharp.Repository(repoPath);
					}
				}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
			return startupTask;
		}

		/// <summary>
		/// Disposes the <see cref="repositoryObject"/>
		/// </summary>
		public void Dispose()
		{
			repositoryObject?.Dispose();
		}
	}
}
