using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;
using TGWebhooks.Core.Configuration;

namespace TGWebhooks.Core.Controllers
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
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// Construst an <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		public AuthorizationController(IGitHubManager gitHubManager, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet]
		public async Task<IActionResult> Begin(CancellationToken cancellationToken)
		{
			if (await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false) != null)
				return View();
			var redirectURI = new Uri(generalConfiguration.RootURL, Url.Action(nameof(Complete)));
			return Redirect(gitHubManager.GetAuthorizationURL(redirectURI).ToString());
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet("Complete")]
		public async Task<IActionResult> Complete(CancellationToken cancellationToken)
		{
			try
			{
				var code = Request.Query["code"];
				await gitHubManager.CompleteAuthorization(code, Response.Cookies, cancellationToken).ConfigureAwait(false);
				return Ok();
			}
			catch (Exception e)
			{
				return BadRequest(e);
			}
		}

		[HttpGet("SignOut")]
		IActionResult SignOut()
		{
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return Ok();
		}

		[HttpGet("SignOut/{number}")]
		IActionResult SignOut(int number)
		{
			gitHubManager.ExpireAuthorization(Response.Cookies);
			return RedirectToAction(null, "PullRequest", number);
		}
	}
}