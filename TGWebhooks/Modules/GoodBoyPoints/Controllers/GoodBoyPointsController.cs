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
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="JsonResult"/></returns>
		[HttpGet]
		public async Task<IActionResult> Index(CancellationToken cancellationToken) => Json(await goodBoyPointsModule.GoodBoyPointsEntries(cancellationToken).ConfigureAwait(false));

		/// <summary>
		/// Set the <paramref name="offset"/> for a <paramref name="prNumber"/>
		/// </summary>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="offset">The <see cref="GoodBoyPointsOffset"/> for <paramref name="prNumber"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IActionResult"/></returns>
		[HttpPost("/{prNumber}")]
		public async Task<IActionResult> SetOffset(int prNumber, [FromBody]GoodBoyPointsOffset offset, CancellationToken cancellationToken)
		{
			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();

			var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(user).ConfigureAwait(false))
				return Forbid();

			await goodBoyPointsModule.SetOffset(prNumber, offset, cancellationToken).ConfigureAwait(false);

			return Ok();
		}
	}
}