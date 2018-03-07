﻿using Microsoft.Extensions.Localization;
using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.Highlander
{
	/// <summary>
	/// Implements the One Per Person <see cref="IModule"/>
	/// </summary>
	sealed class HighlanderModule : IModule, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("ec74d6d5-c0ac-46d2-bcec-f52494e2e8c6");

		/// <inheritdoc />
		public string Name => "One Pull Per Person";

		/// <inheritdoc />
		public string Description => "Only allows one pull request to be open at a time per GitHub user. Maintainers exempt";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="HighlanderModule"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="HighlanderModule"/>
		/// </summary>
		readonly IStringLocalizer<HighlanderModule> stringLocalizer;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Construct a <see cref="HighlanderModule"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		public HighlanderModule(IGitHubManager gitHubManager, IStringLocalizer<HighlanderModule> stringLocalizer)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public async Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			if (payload.Action != "opened")
				throw new NotSupportedException();

			if (await gitHubManager.UserHasWriteAccess(payload.PullRequest.Base.Repository.Owner.Login, payload.PullRequest.Base.Repository.Name, payload.PullRequest.User, cancellationToken).ConfigureAwait(false))
				return;

			var allPrs = await gitHubManager.GetOpenPullRequests(payload.PullRequest.Base.Repository.Owner.Login, payload.PullRequest.Base.Repository.Name, cancellationToken).ConfigureAwait(false);
			string result = null;
			foreach (var I in allPrs.Where(x => x.User.Id == payload.PullRequest.User.Id && x.Id != payload.PullRequest.Id).Select(x => x.Number))
				result = (result != null) ? result + String.Format(CultureInfo.InvariantCulture, ", #{0}", I) : String.Format(CultureInfo.InvariantCulture, "#{0}", I);

			if(result != null)
			{
				await gitHubManager.CreateComment(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, stringLocalizer["TooManyPRs", result], cancellationToken).ConfigureAwait(false);
				await gitHubManager.Close(payload.PullRequest.Base.Repository.Id, payload.PullRequest.Number, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;
	}
}
