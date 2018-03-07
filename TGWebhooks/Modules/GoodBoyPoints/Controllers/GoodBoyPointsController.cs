using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.GoodBoyPoints.Controllers
{
	/// <summary>
	/// Lookup goodboy points
	/// </summary>
	[Route("GoodBoyPoints")]
    public sealed class GoodBoyPointsController : Controller
	{
		/// <summary>
		/// The <see cref="GoodBoyPointsModule"/> for the <see cref="GoodBoyPointsController"/>
		/// </summary>
		readonly GoodBoyPointsModule goodBoyPointsModule;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="GoodBoyPointsController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Construct a <see cref="GoodBoyPointsController"/>
		/// </summary>
		/// <param name="goodBoyPointsModule">The value of <see cref="goodBoyPointsModule"/></param>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		public GoodBoyPointsController(GoodBoyPointsModule goodBoyPointsModule, IGitHubManager gitHubManager)
		{
			this.goodBoyPointsModule = goodBoyPointsModule ?? throw new ArgumentNullException(nameof(goodBoyPointsModule));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
		}

		/// <summary>
		/// Handle a HTTP GET to the <see cref="GoodBoyPointsController"/>
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="JsonResult"/></returns>
		[HttpGet("{owner}/{name}")]
		public async Task<IActionResult> Index(string owner, string name, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var repo = await gitHubManager.GetRepository(owner, name, cancellationToken).ConfigureAwait(false);
			return Json(await goodBoyPointsModule.GoodBoyPointsEntries(repo.Id, cancellationToken).ConfigureAwait(false));
		}

		/// <summary>
		/// Set the <paramref name="offset"/> for a <paramref name="prNumber"/>
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="offset">The <see cref="GoodBoyPointsOffset"/> for <paramref name="prNumber"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> SetOffset(string owner, string name, int prNumber, [FromBody]GoodBoyPointsOffset offset, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			var token = await gitHubManager.CheckAuthorization(owner, name, Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();
			
			var user = await gitHubManager.GetUser(token).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(owner, name, user, cancellationToken).ConfigureAwait(false))
				return Forbid();

			var pullRequest = await gitHubManager.GetPullRequest(owner, name, prNumber, cancellationToken).ConfigureAwait(false);
			await goodBoyPointsModule.SetOffset(pullRequest, offset, cancellationToken).ConfigureAwait(false);

			return Ok();
		}
	}
}