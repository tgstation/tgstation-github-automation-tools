using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Core;

namespace TGWebhooks.Modules.SignOff.Controllers
{
	/// <summary>
	/// Handles <see cref="PullRequestSignOff"/>s and vetos
	/// </summary>
    public sealed class SignOffController : Controller
	{
		/// <summary>
		/// The <see cref="SignOffModule"/> for the <see cref="SignOffController"/>
		/// </summary>
		readonly SignOffModule signOffModule;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="SignOffController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IAutoMergeHandler"/> for the <see cref="SignOffController"/>
		/// </summary>
		readonly IAutoMergeHandler autoMergeHandler;

		/// <summary>
		/// Construct a <see cref="SignOffController"/>
		/// </summary>
		/// <param name="signOffModule">The value of <see cref="signOffModule"/></param>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="autoMergeHandler">The value of <see cref="autoMergeHandler"/></param>
		public SignOffController(SignOffModule signOffModule, IGitHubManager gitHubManager, IAutoMergeHandler autoMergeHandler)
		{
			this.signOffModule = signOffModule ?? throw new ArgumentNullException(nameof(signOffModule));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.autoMergeHandler = autoMergeHandler ?? throw new ArgumentNullException(nameof(autoMergeHandler));
		}

		/// <summary>
		/// Set the <see cref="PullRequestSignOff"/> for a <paramref name="prNumber"/>
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("SignOff/{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> SignOff(string owner, string name, int prNumber, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var token = await gitHubManager.CheckAuthorization(owner, name, Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();
			
			var user = await gitHubManager.GetUser(token).ConfigureAwait(false);
			
			if (!await gitHubManager.UserHasWriteAccess(owner, name, user, cancellationToken).ConfigureAwait(false))
				return Forbid();

			var pr = await gitHubManager.GetPullRequest(owner, name, prNumber, cancellationToken).ConfigureAwait(false);

#if !ENABLE_SELF_SIGN
			//no self signing
			if (pr.User.Id == user.Id)
				return Forbid();
#endif

			await signOffModule.SignOff(pr, user, token, cancellationToken).ConfigureAwait(false);

			autoMergeHandler.RecheckPullRequest(pr);

			return Json(new object());
		}

		/// <summary>
		/// Veto the <see cref="PullRequestSignOff"/> for a <paramref name="prNumber"/>
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("SignOff/Veto/{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> Veto(string owner, string name, int prNumber, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var token = await gitHubManager.CheckAuthorization(owner, name, Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();
			
			var user = await gitHubManager.GetUser(token).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(owner, name, user, cancellationToken).ConfigureAwait(false))
				return Forbid();

			var pr = await gitHubManager.GetPullRequest(owner, name, prNumber, cancellationToken).ConfigureAwait(false);
			await signOffModule.VetoSignOff(pr, cancellationToken).ConfigureAwait(false);

			return Json(new object());
		}
	}
}