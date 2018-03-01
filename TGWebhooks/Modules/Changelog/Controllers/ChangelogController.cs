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
		/// <param name="prNumber">The <see cref="PullRequest.Number"/></param>
		/// <param name="requireChangelogEntry">The <see cref="RequireChangelogEntry"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("Changelog/{prNumber}")]
		public async Task<IActionResult> SetRequirement(int prNumber, [FromBody] RequireChangelogEntry requireChangelogEntry, CancellationToken cancellationToken)
		{
			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();

			var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(user).ConfigureAwait(false))
				return Forbid();
			
			await changelogModule.SetRequirement(prNumber, requireChangelogEntry, cancellationToken).ConfigureAwait(false);

			autoMergeHandler.RecheckPullRequest(prNumber);

			return Json(new object());
		}
	}
}