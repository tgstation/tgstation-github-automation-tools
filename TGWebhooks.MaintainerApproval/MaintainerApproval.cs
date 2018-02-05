using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

namespace TGWebhooks.MaintainerApproval
{
	/// <summary>
	/// <see cref="IPlugin"/> for a <see cref="IMergeRequirement"/> that requires at least one maintainer approval and no outstanding changes requested
	/// </summary>
	public class MaintainerApproval : IPlugin, IMergeRequirement
	{
		/// <inheritdoc />
		public bool Enabled { get; set; }

		/// <inheritdoc />
		public Guid Uid => new Guid("8d8122d0-ad0d-4a91-977f-204d617efd04");

		/// <inheritdoc />
		public string Name => "Maintainer Approval";

		/// <inheritdoc />
		public string Description => "Merge requirement for having at least one maintainer approval and no outstanding changes requested";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="MaintainerApproval"/>
		/// </summary>
		IGitHubManager gitHubManager;

		/// <inheritdoc />
		public void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager webRequestManager, IDataStore dataStore)
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

			var reviews = await gitHubManager.GetPullRequestReviews(pullRequest);

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

			await Task.WhenAll(userCheckups.Select(x => x.Value));

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
