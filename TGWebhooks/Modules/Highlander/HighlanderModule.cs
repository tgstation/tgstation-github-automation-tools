using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.Highlander
{
	/// <summary>
	/// Implements the One Per Person <see cref="IModule"/>
	/// </summary>
	public sealed class HighlanderModule : IModule, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("ec74d6d5-c0ac-46d2-bcec-f52494e2e8c6");

		/// <inheritdoc />
		public string Name => "One Per Person";

		/// <inheritdoc />
		public string Description => "Only allows one pull request to be open at a time per GitHub user";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="HighlanderModule"/>
		/// </summary>
		IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="HighlanderModule"/>
		/// </summary>
		IStringLocalizer stringLocalizer;

		/// <summary>
		/// Ensure that <see cref="Configure(ILogger, IRepository, IGitHubManager, IIOManager, IWebRequestManager, IDataStore, IStringLocalizer)"/> was called
		/// </summary>
		void CheckConfigured()
		{
			if (gitHubManager == null)
				throw new InvalidOperationException("Configure wasn't called!");
		}

		/// <inheritdoc />
		public void Configure(ILogger logger, IRepository repository, IGitHubManager gitHubManager, IIOManager ioManager, IWebRequestManager webRequestManager, IDataStore dataStore, IStringLocalizer stringLocalizer)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			CheckConfigured();
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
			if (payload.Action != "opened")
				throw new NotSupportedException();

			var allPrs = await gitHubManager.GetOpenPullRequests().ConfigureAwait(false);
			if (allPrs.Any(x => x.User.Id == payload.PullRequest.User.Id && x.Id != payload.PullRequest.Id))
			{
				await gitHubManager.CreateComment(payload.PullRequest.Number, stringLocalizer["TooManyPRs"]);
				await gitHubManager.Close(payload.PullRequest.Number);
			}
		}
	}
}
