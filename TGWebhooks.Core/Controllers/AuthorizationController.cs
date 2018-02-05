using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TGWebhooks.Api;
using System.Threading;

using StreamReader = System.IO.StreamReader;
using Newtonsoft.Json.Linq;

namespace TGWebhooks.Core.Controllers
{
	/// <summary>
	/// Handler for the <see cref="IGitHubManager"/> Oauth flow
	/// </summary>
	[Produces("application/json")]
	[Route("Authorize")]
    public sealed class AuthorizationController : Controller
    {
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="AuthorizationController"/>
		/// </summary>
		IGitHubManager gitHubManager;

		/// <summary>
		/// Construst an <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		public AuthorizationController(IGitHubManager gitHubManager)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet]
		public IActionResult Begin()
		{
			var redirectURI = new Uri(Url.AbsoluteAction(nameof(Complete), nameof(AuthorizationController)));
			return Redirect(gitHubManager.GetAuthorizationURL(redirectURI).ToString());
		}

		/// <summary>
		/// Handle a GET to the <see cref="AuthorizationController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet]
		[Route("Complete")]
		public async Task<IActionResult> Complete(CancellationToken cancellationToken)
		{
			string jsonString;
			using (var reader = new StreamReader(Request.Body))
				jsonString = await reader.ReadToEndAsync().ConfigureAwait(false);
			try
			{
				var json = new JObject(jsonString);
				var code = (string)json["code"];
				await gitHubManager.CompleteAuthorization(code, Response.Cookies, cancellationToken).ConfigureAwait(false);
				return Ok();
			}
			catch (Exception e)
			{
				return BadRequest(e);
			}
		}
	}
}