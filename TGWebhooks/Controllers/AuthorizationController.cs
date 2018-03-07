using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Globalization;
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
		[HttpGet("Login/{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> Begin(string owner, string name, int prNumber, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (await gitHubManager.CheckAuthorization(owner, name, Request.Cookies, cancellationToken).ConfigureAwait(false) != null)
				return RedirectToAction("ReviewPullRequest", "PullRequest", new { number = prNumber });
			
			var redirectURI = Url.Action(nameof(Complete));
			return Redirect(gitHubManager.GetAuthorizationURL(new Uri(String.Format(CultureInfo.InvariantCulture, "https://{0}{1}", Request.Host, redirectURI)), owner, name, prNumber).ToString());
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet("Complete")]
		public async Task<IActionResult> Complete(CancellationToken cancellationToken)
		{
			if (!Request.Query.TryGetValue("state", out StringValues state) || state.Count != 1)
				return BadRequest();

			var splits = state[0].Split('/');

			if (splits.Length != 3)
				return BadRequest();

			var owner = splits[0];
			var name = splits[1];
			if (!Int32.TryParse(splits[2], out int number))
				return BadRequest();

			try
			{
				var code = Request.Query["code"];
				await gitHubManager.CompleteAuthorization(code, Response.Cookies, cancellationToken).ConfigureAwait(false);
				return RedirectToAction("ReviewPullRequest", "PullRequest", new { owner, name, number });
			}
			catch (Exception e)
			{
				return BadRequest(e);
			}
		}

		/// <summary>
		/// Signs out the user and closes their window
		/// </summary>
		/// <param name="owner">The <see cref="Octokit.Repository.Owner"/> for the operation</param>
		/// <param name="name">The <see cref="Octokit.Repository.Name"/> for the operation</param>
		/// <param name="prNumber">The <see cref="Octokit.PullRequest.Number"/> to redirect to, defaults to the first open pull request</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>An <see cref="RedirectToActionResult"/></returns>
		[HttpGet("SignOut/{owner}/{name}/{prNumber}")]
		public async Task<IActionResult> SignOut(string owner, string name, int prNumber, CancellationToken cancellationToken)
		{
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return RedirectToAction("ReviewPullRequest", "PullRequest", new { owner, name, number = prNumber > 0 ? prNumber : (await gitHubManager.GetOpenPullRequests(owner, name, cancellationToken).ConfigureAwait(false))[0].Number });
		}

		/// <summary>
		/// Signs out the user and closes their window
		/// </summary>
		/// <returns>An <see cref="RedirectToActionResult"/></returns>
		[HttpGet("SignOut")]
		public IActionResult SignOut()
		{
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return View();
		}
	}
}