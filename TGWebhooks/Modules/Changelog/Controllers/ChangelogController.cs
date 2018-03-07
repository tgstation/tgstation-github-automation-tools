using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Core;

namespace TGWebhooks.Modules.Changelog.Controllers
{
	/// <summary>
	/// Handles <see cref="Models.Changelog"/> requirements
	/// </summary>
    public sealed class ChangelogController : Controller
	{
		/// <summary>
		/// The <see cref="ChangelogModule"/> for the <see cref="ChangelogController"/>
		/// </summary>
		readonly ChangelogModule changelogModule;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="ChangelogController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IAutoMergeHandler"/> for the <see cref="ChangelogController"/>
		/// </summary>
		readonly IAutoMergeHandler autoMergeHandler;

		/// <summary>
		/// Construct a <see cref="ChangelogController"/>
		/// </summary>
		/// <param name="changelogModule">The value of <see cref="changelogModule"/></param>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="autoMergeHandler">The value of <see cref="autoMergeHandler"/></param>
		public ChangelogController(ChangelogModule changelogModule, IGitHubManager gitHubManager, IAutoMergeHandler autoMergeHandler)
		{
			this.changelogModule = changelogModule ?? throw new ArgumentNullException(nameof(changelogModule));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.autoMergeHandler = autoMergeHandler ?? throw new ArgumentNullException(nameof(autoMergeHandler));
		}

		/// <summary>
		/// Set the <see cref="RequireChangelogEntry"/> for a <paramref name="prNumber"/>
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="requireChangelogEntry">The <see cref="RequireChangelogEntry"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("Changelog/{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> SetRequirement(string owner, string name, int prNumber, [FromBody] RequireChangelogEntry requireChangelogEntry, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (owner == null)
				throw new ArgumentNullException(nameof(name));

			var token = await gitHubManager.CheckAuthorization(owner, name, Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();

			var user = await gitHubManager.GetUser(token).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(owner, name, user, cancellationToken).ConfigureAwait(false))
				return Forbid();

			var pr = await gitHubManager.GetPullRequest(owner, name, prNumber, cancellationToken).ConfigureAwait(false);
			await changelogModule.SetRequirement(pr, requireChangelogEntry, cancellationToken).ConfigureAwait(false);

			autoMergeHandler.RecheckPullRequest(pr);

			return Json(new object());
		}
	}
}