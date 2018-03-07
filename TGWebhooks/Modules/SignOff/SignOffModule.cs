using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.SignOff
{
	/// <summary>
	/// <see cref="IModule"/> containing the Maintainer Sign Off <see cref="IMergeRequirement"/>
	/// </summary>
	public sealed class SignOffModule : IModule, IMergeRequirement, IPayloadHandler<PullRequestEventPayload>
	{
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
			var signOff = await dataStore.ReadData<PullRequestSignOff>(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);

			var result = new AutoMergeStatus() {
				RequiredProgress = 1,
				Progress = signOff.AccessToken != null ? 1 : 0,
				MergerAccessToken = signOff.AccessToken
			};

			if (signOff.AccessToken != null) {
				var user = await gitHubManager.GetUser(signOff.AccessToken).ConfigureAwait(false);
				result.Notes.Add(stringLocalizer["Signer", user.Login]);
			}
			return result;
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public async Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (viewBag == null)
				throw new ArgumentNullException(nameof(viewBag));

			if (!viewBag.IsMaintainer)
				return;

#if !ENABLE_SELF_SIGN
			if (viewBag.UserIsAuthor)
				return;
#endif

			//take priority
			((IList<string>)viewBag.ModuleViews).Insert(0, "/Modules/SignOff/Views/SignOff.cshtml");

			viewBag.SignOffHeader = stringLocalizer["SignOffHeader"];
			viewBag.SignOffLabel = stringLocalizer["SignOffLabel"];
			viewBag.VetoLabel = stringLocalizer["VetoLabel"];
			viewBag.SignedBy = stringLocalizer["SignedBy"];
			viewBag.SignOffDisclaimer = stringLocalizer["Disclaimer"];

			var signer = await dataStore.ReadData<PullRequestSignOff>(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);
			if (signer.AccessToken != null)
				viewBag.Signer = (await gitHubManager.GetUser(signer.AccessToken).ConfigureAwait(false)).Login;
			else
				viewBag.Signer = null;
		}

		/// <summary>
		/// Vetos the <see cref="PullRequestSignOff"/> for a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to veto</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public Task VetoSignOff(PullRequest pullRequest, CancellationToken cancellationToken) => EraseAndDismissReviews(pullRequest, stringLocalizer["SignOffVetod"], cancellationToken);

		/// <summary>
		/// Adds the <see cref="PullRequestSignOff"/> for a given <paramref name="pullRequest"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to sign off</param>
		/// <param name="user">The <see cref="User"/> doing the signing</param>
		/// <param name="token">The api token for <paramref name="user"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public async Task SignOff(PullRequest pullRequest, User user, string token, CancellationToken cancellationToken)
		{
			if (pullRequest == null)
				throw new ArgumentNullException(nameof(pullRequest));
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			await dataStore.WriteData(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, new PullRequestSignOff { AccessToken = token }, cancellationToken).ConfigureAwait(false);
			await gitHubManager.ApprovePullRequest(pullRequest, stringLocalizer["SignerTag", user.Login], cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Erases the sign off and dismisses the 'approved' reviews
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> to un-sign</param>
		/// <param name="message">The dismissal message</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		async Task EraseAndDismissReviews(PullRequest pullRequest, string message, CancellationToken cancellationToken)
		{
			var reviewsTask = gitHubManager.GetPullRequestReviews(pullRequest, cancellationToken);
			var botLoginTask = gitHubManager.GetUser(null);
			await dataStore.WriteData(pullRequest.Number.ToString(CultureInfo.InvariantCulture), pullRequest.Base.Repository.Id, new PullRequestSignOff(), cancellationToken).ConfigureAwait(false);
			var reviews = await reviewsTask.ConfigureAwait(false);
			var botLogin = await botLoginTask.ConfigureAwait(false);

			await Task.WhenAll(
				reviews.Where(
					x => x.User.Id == botLogin.Id
					&& x.State.Value == PullRequestReviewState.Approved
				).Select(
					x => gitHubManager.DismissReview(pullRequest, x, message, cancellationToken)
				)).ConfigureAwait(false);
		}

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			
			if(payload.Action != "edited")
				throw new NotSupportedException();

			var signOff = await dataStore.ReadData<PullRequestSignOff>(payload.PullRequest.Number.ToString(CultureInfo.InvariantCulture), payload.PullRequest.Base.Repository.Id, cancellationToken).ConfigureAwait(false);

			if (signOff.AccessToken == null)
				return;
			
			await EraseAndDismissReviews(payload.PullRequest, stringLocalizer["SignOffNulled"], cancellationToken).ConfigureAwait(false);
		}
	}
}
