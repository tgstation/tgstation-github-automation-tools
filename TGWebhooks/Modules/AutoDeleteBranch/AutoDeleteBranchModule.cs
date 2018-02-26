using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.AutoDeleteBranch
{
	/// <summary>
	/// Contains the <see cref="IPayloadHandler{TPayload}"/> to delete merged branches
	/// </summary>
	sealed class AutoDeleteBranchModule : IModule, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public bool Enabled { get; set; }

		/// <inheritdoc />
		public Guid Uid => new Guid("d4235804-4cb8-4e81-bc7f-f08f8c2918d6");

		/// <inheritdoc />
		public string Name => "Auto Delete Branch";

		/// <inheritdoc />
		public string Description => "Deletes branches of merged pull requests from the same repository";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <summary>
		/// The <see cref="IGitHubClient"/> for the <see cref="AutoDeleteBranchModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Construct an <see cref="AutoDeleteBranchModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		public AutoDeleteBranchModule(IGitHubManager gitHubManager)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));

			if (payload.Action != "closed" || !payload.PullRequest.Merged)
				throw new NotSupportedException();

			if (payload.PullRequest.Base.Repository.Id != payload.PullRequest.Head.Repository.Id)
				return;

			await gitHubManager.DeleteBranch(payload.PullRequest.Head.Ref);
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
