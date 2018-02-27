using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.ReviewManager
{
	/// <summary>
	/// <see cref="IModule"/> for managing maintainer reviews
	/// </summary>
	public sealed class ReviewManagerModule : IModule, IMergeRequirement
	{
		/// <inheritdoc />
		public bool Enabled { get; set; }
		/// <inheritdoc />
		public Guid Uid => new Guid("8d8122d0-ad0d-4a91-977f-204d617efd04");

		/// <inheritdoc />
		public string Name => stringLocalizer["Name"];

		/// <inheritdoc />
		public string Description => stringLocalizer["Description"];

		/// <inheritdoc />
		public string RequirementDescription => stringLocalizer["RequirementDescription"];

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => new List<IMergeRequirement> { this };

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="ReviewManagerModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="ReviewManagerModule"/>
		/// </summary>
		readonly IStringLocalizer<ReviewManagerModule> stringLocalizer;

		/// <summary>
		/// Construct a <see cref="ReviewManagerModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public ReviewManagerModule(IGitHubManager gitHubManager, IStringLocalizer<ReviewManagerModule> stringLocalizer)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			return Enumerable.Empty<IPayloadHandler<TPayload>>();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

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
				result.Notes.Add(stringLocalizer["RequestedChanges", I.Login]);
			}

			result.RequiredProgress = Math.Max(result.RequiredProgress, 1);

			return result;
		}
	}
}
