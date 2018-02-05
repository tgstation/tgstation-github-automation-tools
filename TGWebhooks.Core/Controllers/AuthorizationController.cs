using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TGWebhooks.Interface;
using System.Threading;

using StreamReader = System.IO.StreamReader;
using Newtonsoft.Json.Linq;

namespace TGWebhooks.Core.Controllers
{
	[Produces("application/json")]
	[Route("Authorize")]
    public sealed class AuthorizationController : Controller
    {
		IGitHubManager gitHubManager;
		public AuthorizationController(IGitHubManager _gitHubManager)
		{
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
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
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the GET</returns>
		[HttpGet]
		[Route("Complete")]
		public async Task<IActionResult> Complete(CancellationToken cancellationToken)
		{
			string jsonString;
			using (var reader = new StreamReader(Request.Body))
				jsonString = await reader.ReadToEndAsync();
			try
			{
				var json = new JObject(jsonString);
				var code = (string)json["code"];
				await gitHubManager.CompleteAuthorization(code);
				return Ok();
			}
			catch (Exception e)
			{
				return BadRequest(e);
			}
		}
	}
}