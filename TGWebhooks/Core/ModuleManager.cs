using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Models;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class ModuleManager : IModuleManager, IDisposable
#pragma warning restore CA1812
	{		
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly ILogger<ModuleManager> logger;
		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IDatabaseContext databaseContext;
		/// <summary>
		/// All the <see cref="IModule"/>s for the <see cref="ModuleManager"/>
		/// </summary>
		readonly IEnumerable<IModule> allModules;

		/// <summary>
		/// Used for creating <see cref="RepositoryContext"/>s
		/// </summary>
		readonly SemaphoreSlim semaphore;

		/// <summary>
		/// The current <see cref="RepositoryContext"/>
		/// </summary>
		RepositoryContext repositoryContext;

		/// <summary>
		/// Construct a <see cref="ModuleManager"/>
		/// </summary>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_databaseContext">The value of <see cref="databaseContext"/></param>
		/// <param name="_allModules">The value of <see cref="allModules"/></param>
		public ModuleManager(ILogger<ModuleManager> _logger, IDatabaseContext _databaseContext, IEnumerable<IModule> _allModules)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			databaseContext = _databaseContext ?? throw new ArgumentNullException(nameof(_databaseContext));
			allModules = _allModules ?? throw new ArgumentNullException(nameof(_allModules));
			semaphore = new SemaphoreSlim(1);
		}

		/// <inheritdoc />
		public void Dispose() => semaphore.Dispose();

		/// <inheritdoc />
		public async Task<IRepositoryContext> UsingRepositoryId(long repositoryId, CancellationToken cancellationToken)
		{
			var context = await SemaphoreSlimContext.Lock(semaphore, cancellationToken).ConfigureAwait(false);
			try
			{
				var dic = await ModuleStatuses(repositoryId, cancellationToken).ConfigureAwait(false);
				var enumerable = dic.Select(x => x.Key);
				repositoryContext = new RepositoryContext(context, enumerable, repositoryId, () => repositoryContext = null);
				return repositoryContext;
			}
			catch
			{
				context.Dispose();
				throw;
			}
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			var repoContext = repositoryContext;
			if (repoContext == null)
				throw new InvalidOperationException("UsingRepositoryId was not called!");
			return repoContext.EnabledModules.SelectMany(x => x.GetPayloadHandlers<TPayload>());
		}

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements {
			get
			{
				var repoContext = repositoryContext;
				if (repoContext == null)
					throw new InvalidOperationException("UsingRepositoryId was not called!");
				return repoContext.EnabledModules.SelectMany(x => x.MergeRequirements);
			}
		}

		/// <inheritdoc />
		public async Task SetModuleEnabled(Guid uid, long repositoryId, bool enabled, CancellationToken cancellationToken)
		{
			ModuleMetadata dbentry;
			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
			{
				dbentry = await databaseContext.ModuleMetadatas.Where(x => x.Uid == uid && x.RepositoryId == repositoryId).ToAsyncEnumerable().First(cancellationToken).ConfigureAwait(false);
				if (enabled && dbentry.Enabled)
					return;
				dbentry.Enabled = enabled;
				await databaseContext.Save(cancellationToken).ConfigureAwait(false);
			}

			logger.LogInformation("Module {0} enabled status set to {1} for repo {2}", uid, enabled, repositoryId);
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken)
		{
			var repoContext = repositoryContext;
			if (repoContext == null)
				throw new InvalidOperationException("UsingRepositoryId was not called!");
			return Task.WhenAll(repositoryContext.EnabledModules.Select(x => x.AddViewVars(pullRequest, (object)viewBag, cancellationToken)));
		}

		/// <inheritdoc />
		public async Task<IDictionary<IModule, bool>> ModuleStatuses(long repositoryId, CancellationToken cancellationToken)
		{
			var repoDic = new Dictionary<IModule, bool>();

			using (await databaseContext.LockToCallStack(cancellationToken).ConfigureAwait(false))
				foreach (var plugin in allModules)
				{
					var query = databaseContext.ModuleMetadatas.Where(x => x.Uid == plugin.Uid && x.RepositoryId == repositoryId);
					var result = await query.ToAsyncEnumerable().FirstOrDefault().ConfigureAwait(false);

					if (result == default(ModuleMetadata))
						repoDic.Add(plugin, true);
					else
						repoDic.Add(plugin, result.Enabled);
				};
			
			return repoDic;
		}
	}
}
