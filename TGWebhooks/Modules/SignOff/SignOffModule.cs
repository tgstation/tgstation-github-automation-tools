using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.SignOff
{
	/// <summary>
	/// <see cref="IModule"/> containing the Maintainer Sign Off <see cref="IMergeRequirement"/>
	/// </summary>
	sealed class SignOffModule : IModule, IMergeRequirement, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public bool Enabled { get; set; }

		/// <inheritdoc />
		public Guid Uid => new Guid("bde81200-a275-4e93-b855-13865f3629fe");

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
		/// The <see cref="IGitHubManager"/> for the <see cref="SignOffModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IDataStore"/> for the <see cref="SignOffModule"/>
		/// </summary>
		readonly IDataStore dataStore;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="SignOffModule"/>
		/// </summary>
		readonly IStringLocalizer<SignOffModule> stringLocalizer;

		/// <summary>
		/// Construct a <see cref="SignOffModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="dataStoreFactory">The <see cref="IDataStoreFactory{TModule}"/> for <see cref="dataStore"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public SignOffModule(IGitHubManager gitHubManager, IDataStoreFactory<SignOffModule> dataStoreFactory, IStringLocalizer<SignOffModule> stringLocalizer)
		{
			dataStore = dataStoreFactory?.CreateDataStore(this) ?? throw new ArgumentNullException(nameof(dataStoreFactory));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public async Task<AutoMergeStatus> EvaluateFor(PullRequest pullRequest, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			var signOff = await dataStore.ReadData<PullRequestSignOff>(pullRequest.Number.ToString(), cancellationToken).ConfigureAwait(false);

			var result = new AutoMergeStatus() {
				RequiredProgress = 1,
				Progress = signOff.AccessToken != null ? 1 : 0,
				MergerAccessToken = signOff.AccessToken
			};

			if (signOff.AccessToken != null) {
				var user = await gitHubManager.GetUserLogin(signOff.AccessToken, cancellationToken).ConfigureAwait(false);
				result.Notes.Add(stringLocalizer["Signer", user.Login]);
			}
			else
				result.Notes.Add(stringLocalizer["NoSignOffs"]);
			return result;
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
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			
			if(payload.Action != "edited")
				throw new NotSupportedException();

			var signOff = await dataStore.ReadData<PullRequestSignOff>(payload.PullRequest.Number.ToString(), cancellationToken).ConfigureAwait(false);

			if (signOff.AccessToken == null)
				return;

			await dataStore.WriteData(payload.PullRequest.Number.ToString(), signOff, cancellationToken).ConfigureAwait(false);

			var botLoginTask = gitHubManager.GetUserLogin(null, cancellationToken);
			var reviews = await gitHubManager.GetPullRequestReviews(payload.PullRequest).ConfigureAwait(false);
			var botLogin = await botLoginTask.ConfigureAwait(false);
			
			await Task.WhenAll(
				reviews.Where(
					x => x.User.Id == botLogin.Id 
					&& x.State.Value == PullRequestReviewState.Approved
				).Select(
					x => gitHubManager.DismissReview(payload.PullRequest, x, stringLocalizer["SignOffNulled"])
				));
		}
	}
}
