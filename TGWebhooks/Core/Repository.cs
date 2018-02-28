using LibGit2Sharp;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Modules;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class Repository : Modules.IRepository, IInitializable, IDisposable
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
		/// The <see cref="IStringLocalizer"/> for the <see cref="Repository"/>
		/// </summary>
		readonly IStringLocalizer<Repository> stringLocalizer;

		/// <summary>
		/// The <see cref="LibGit2Sharp.Repository"/> for the <see cref="Repository"/>
		/// </summary>
		LibGit2Sharp.Repository repositoryObject;

		/// <summary>
		/// Used for guarding access to the <see cref="Repository"/>
		/// </summary>
		SemaphoreSlim semaphore;

		/// <summary>
		/// Create an <see cref="Identity"/> given a <see cref="User"/>
		/// </summary>
		/// <param name="user">The <see cref="User"/> to derive the <see cref="Identity"/> from</param>
		/// <returns>A new <see cref="Identity"/></returns>
		static Identity CreateIdentity(User user) => user == null ? new Identity(Application.UserAgent, String.Concat(Application.UserAgent, "@users.noreply.github.com")) : new Identity(user.Name, user.Email);

		/// <summary>
		/// Construct a <see cref="Repository"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the <see cref="GitHubConfiguration"/> to use for determining the <see cref="repositoryObject"/>'s path</param>
		/// <param name="_logger">The <see cref="ILogger"/> to use for setting up the <see cref="Repository"/></param>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		public Repository(IOptions<GitHubConfiguration> gitHubConfigurationOptions, ILogger<Repository> _logger, IIOManager _ioManager, IStringLocalizer<Repository> _stringLocalizer)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			stringLocalizer = _stringLocalizer ?? throw new ArgumentNullException(nameof(_stringLocalizer));
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
		public Task Initialize(CancellationToken cancellationToken) => Task.Factory.StartNew(async () =>
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

		/// <inheritdoc />
		public Task<string> CreatePullRequestWorkingCommit(PullRequest pullRequest, User committer, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		{
			var refspecs = new List<string>();
			var prBranchName = String.Format(CultureInfo.InvariantCulture, "pr-{0}", pullRequest.Number);
			refspecs.Add(String.Format(CultureInfo.InvariantCulture, "pull/{0}/head:{1}", pullRequest.Number, prBranchName));
			const string origin = "origin";
			var originRemote = repositoryObject.Network.Remotes[origin];
			refspecs.AddRange(originRemote.FetchRefSpecs.Select(X => X.Specification));

			//standard cleanup
			repositoryObject.RemoveUntrackedFiles();

			cancellationToken.ThrowIfCancellationRequested();

			Commands.Fetch(repositoryObject, origin, refspecs, new FetchOptions()
			{
				OnProgress = (a) => !cancellationToken.IsCancellationRequested,
				OnTransferProgress = (a) => !cancellationToken.IsCancellationRequested,
				RepositoryOperationStarting = (a) => !cancellationToken.IsCancellationRequested
			}, stringLocalizer["FetchLogMessage", pullRequest.Number]);

			cancellationToken.ThrowIfCancellationRequested();
			Commands.Checkout(repositoryObject, pullRequest.Base.Sha);

			cancellationToken.ThrowIfCancellationRequested();
			var result = repositoryObject.Merge(pullRequest.Head.Sha, new LibGit2Sharp.Signature(CreateIdentity(null), DateTimeOffset.UtcNow), new MergeOptions
			{
				CommitOnSuccess = true,
				FailOnConflict = true,
				FastForwardStrategy = FastForwardStrategy.NoFastForward
			});

			//safe to delete that branch now
			repositoryObject.Branches.Remove(prBranchName);

			if (result.Status != MergeStatus.NonFastForward)
				return null;    //abork, abork

			cancellationToken.ThrowIfCancellationRequested();
			var headCommit = repositoryObject.Head.Tip;
			//now soft reset and prepare to squash
			repositoryObject.Reset(ResetMode.Mixed, headCommit.Parents.First());

			cancellationToken.ThrowIfCancellationRequested();

			//a
			Commands.Stage(repositoryObject, "*");

			cancellationToken.ThrowIfCancellationRequested();

			var dto = DateTimeOffset.UtcNow;
			var authorSig = new LibGit2Sharp.Signature(CreateIdentity(pullRequest.User), dto);
			var committerSig = new LibGit2Sharp.Signature(CreateIdentity(committer), dto);
			return repositoryObject.Commit(String.Format(CultureInfo.InvariantCulture, "{0} - (#{1}){2}{3}", pullRequest.Title, pullRequest.Number, Environment.NewLine, pullRequest.Body), authorSig, committerSig, new CommitOptions { PrettifyMessage = true }).Sha;
		}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public Task<SemaphoreSlimContext> LockToCallStack(CancellationToken cancellationToken)
		{
			return SemaphoreSlimContext.Lock(semaphore, cancellationToken);
		}

		/// <inheritdoc />
		public Task<string> CommitChanges(IEnumerable<string> pathsToStage, string message, User author, User committer, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
		   {
			   foreach (var I in pathsToStage)
				   Commands.Stage(repositoryObject, I);
			   var dto = DateTimeOffset.UtcNow;
			   var authorSig = new LibGit2Sharp.Signature(CreateIdentity(author), dto);
			   var commiterSig = new LibGit2Sharp.Signature(CreateIdentity(committer), dto);
			   return repositoryObject.Commit(message, authorSig, commiterSig).Sha;
		   }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

		/// <inheritdoc />
		public Task Push(string remote, string branch, string commit, string token, bool force, CancellationToken cancellationToken) => Task.Factory.StartNew(() =>
			{
				try
				{
					var remoteObject = repositoryObject.Network.Remotes.Where(x => x.Url == remote).FirstOrDefault();
					if (remoteObject == default(Remote))
					{
						repositoryObject.Network.Remotes.Remove("tempRemote");
						remoteObject = repositoryObject.Network.Remotes.Add("tempRemote", remote);
					}
					repositoryObject.Network.Push(remoteObject, commit, String.Format(CultureInfo.InvariantCulture, "{0}{1}:{2}", force ? "+" : null, commit, branch), new PushOptions()
					{
						OnPushTransferProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						OnPackBuilderProgress = (a, b, c) => !cancellationToken.IsCancellationRequested,
						OnNegotiationCompletedBeforePush = (a) => !cancellationToken.IsCancellationRequested
					});
				}
				catch (UserCancelledException)
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
			}, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
	}
}
