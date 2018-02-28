using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Configuration;

namespace TGWebhooks.Controllers
{
	/// <summary>
	/// Handler for the <see cref="IGitHubManager"/> Oauth flow
	/// </summary>
	[Route("Authorize")]
    public sealed class AuthorizationController : Controller
    {
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="AuthorizationController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="AuthorizationController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construst an <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		public AuthorizationController(IGitHubManager gitHubManager, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet("Login/{prNumber}")]
		public async Task<IActionResult> Begin(int prNumber, CancellationToken cancellationToken)
		{
			if (await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false) != null)
				return RedirectToAction("ReviewPullRequest", "PullRequest", new { number = prNumber });
			var redirectURI = new Uri(generalConfiguration.RootURL, Url.Action(nameof(Complete), prNumber));
			return Redirect(String.Concat(gitHubManager.GetAuthorizationURL(redirectURI).ToString()));
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="prNumber">A <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet("Complete/{prNumber}")]
		public async Task<IActionResult> Complete(int prNumber, CancellationToken cancellationToken)
		{
			try
			{
				var code = Request.Query["code"];
				await gitHubManager.CompleteAuthorization(code, Response.Cookies, cancellationToken).ConfigureAwait(false);
				return RedirectToAction("ReviewPullRequest", "PullRequest", new { number = prNumber });
			}
			catch (Exception e)
			{
				return BadRequest(e);
			}
		}

		/// <summary>
		/// Signs out the user and closes their window
		/// </summary>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/> to redirect to, defaults to the first open pull request</param>
		/// <returns>An <see cref="RedirectToActionResult"/></returns>
		[HttpGet("SignOut/{prNumber}")]
		public async Task<IActionResult> SignOut(int prNumber)
		{
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return RedirectToAction("ReviewPullRequest", "PullRequest", new { number = prNumber > 0 ? prNumber : (await gitHubManager.GetOpenPullRequests().ConfigureAwait(false)).First().Number });
		}

		/// <summary>
		/// Signs out the user and closes their window
		/// </summary>
		/// <returns>An <see cref="RedirectToActionResult"/></returns>
		[HttpGet("SignOut")]
		public async Task<IActionResult> SignOut()
		{
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return RedirectToAction("ReviewPullRequest", "PullRequest", new { number = (await gitHubManager.GetOpenPullRequests().ConfigureAwait(false)).First().Number });
		}
	}
}