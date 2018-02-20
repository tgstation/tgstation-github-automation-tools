using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

namespace TGWebhooks.Modules.MaintainerApproval
{
	/// <summary>
	/// <see cref="IModule"/> for a <see cref="IMergeRequirement"/> that requires at least one maintainer approval and no outstanding changes requested
	/// </summary>
	public sealed class MaintainerApprovalModule : IModule, IMergeRequirement
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("8d8122d0-ad0d-4a91-977f-204d617efd04");

		/// <inheritdoc />
		public string Name => "Maintainer Approval";

		/// <inheritdoc />
		public string Description => "Merge requirement for having at least one maintainer approval and no outstanding changes requested";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="MaintainerApproval"/>
		/// </summary>
		IGitHubManager gitHubManager;

		/// <inheritdoc />
		public void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager webRequestManager, IDataStore dataStore, IStringLocalizer stringLocalizer)
		{
			this.gitHubManager = gitHubManager;
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			return Enumerable.Empty<IPayloadHandler<TPayload>>();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (gitHubManager == null)
				throw new InvalidOperationException("Configure() wasn't called!");

			var reviews = await gitHubManager.GetPullRequestReviews(pullRequest).ConfigureAwait(false);

			var approvers = new List<User>();
			var critics = new List<User>();

			var userCheckups = new Dictionary<string, Task<bool>>();

			foreach(var I in reviews)
			{
				void CheckupUser()
				{
					if (!userCheckups.ContainsKey(I.User.Login))
						userCheckups.Add(I.User.Login, gitHubManager.UserHasWriteAccess(I.User));
				}
				if (I.State.Value == PullRequestReviewState.Approved)
				{
					approvers.Add(I.User);
					CheckupUser();
				}
				else if (I.State.Value == PullRequestReviewState.ChangesRequested)
				{
					critics.Add(I.User);
					CheckupUser();
				}
			}

			var result = new AutoMergeStatus();

			await Task.WhenAll(userCheckups.Select(x => x.Value)).ConfigureAwait(false);

			foreach(var I in approvers)
			{
				if (!userCheckups[I.Login].Result)
					continue;

				++result.Progress;
			}

			result.RequiredProgress = result.Progress;

			foreach(var I in critics)
			{
				if (!userCheckups[I.Login].Result)
					continue;

				++result.RequiredProgress;
				result.Notes.Add(String.Format(CultureInfo.CurrentCulture, "{0} requested changes", I.Login));
			}

			result.RequiredProgress = Math.Max(result.RequiredProgress, 1);

			return result;
		}
	}
}
