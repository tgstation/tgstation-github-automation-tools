using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using TGWebhooks.Api;
using TGWebhooks.Core.Configuration;

namespace TGWebhooks.Core.Controllers
{
	/// <summary>
	/// Main <see cref="Octokit.PullRequest"/> management interface
	/// </summary>
	[Route(Route)]
	public sealed class PullRequestController : Controller
    {
		/// <summary>
		/// The route to the primary controller action formatted with a pull request number
		/// </summary>
		public const string Route = "PullRequest";
		readonly IGitHubManager gitHubManager;
		readonly IStringLocalizer<PullRequestController> stringLocalizer;
		readonly GeneralConfiguration generalConfiguration;

		public PullRequestController(IGitHubManager gitHubManager, IStringLocalizer<PullRequestController> stringLocalizer, IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
		}

		[HttpGet("{number}")]
		public async Task<IActionResult> ReviewPullRequest(int number, CancellationToken cancellationToken)
		{
			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
			{
				ViewBag.AuthHref = String.Concat(generalConfiguration.RootURL.ToString(), "Authorize");
				ViewBag.AuthTitle = "Sign In With GitHub";
			}
			else
			{
				var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);
				ViewBag.AuthHref = String.Concat(generalConfiguration.RootURL.ToString(), "SignOut/", number);
				ViewBag.AuthTitle = stringLocalizer["SignOutText", user.Login];
			}
			return View();
		}
	}
}
