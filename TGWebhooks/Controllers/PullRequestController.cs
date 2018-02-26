using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Modules;
using TGWebhooks.Configuration;

namespace TGWebhooks.Controllers
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

		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="PullRequestController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="PullRequestController"/>
		/// </summary>
		readonly IStringLocalizer<PullRequestController> stringLocalizer;
		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="PullRequestController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="PullRequestController"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;

		/// <summary>
		/// Construct a <see cref="PullRequestController"/>
		/// </summary>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		/// <param name="githubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		public PullRequestController(IGitHubManager gitHubManager, IStringLocalizer<PullRequestController> stringLocalizer, IOptions<GeneralConfiguration> generalConfigurationOptions, IOptions<GitHubConfiguration> githubConfigurationOptions)
		{
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			gitHubConfiguration = githubConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(githubConfigurationOptions));
		}

		/// <summary>
		/// Review automation information for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		/// <param name="number">The <see cref="Octokit.PullRequest.Number"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="ViewResult"/></returns>
		[HttpGet("{number}")]
		public async Task<IActionResult> ReviewPullRequest(int number, CancellationToken cancellationToken)
		{
			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			ViewBag.Title = stringLocalizer["PullRequest", number];
			ViewBag.Modules = stringLocalizer["ManageModules"];
			ViewBag.PRNumber = number;
			ViewBag.RepoOwner = gitHubConfiguration.RepoOwner;
			ViewBag.RepoName = gitHubConfiguration.RepoName;

			var pr = await gitHubManager.GetPullRequest(number).ConfigureAwait(false);
			ViewBag.PullRequestAuthor = pr.User.Login;
			ViewBag.PullRequestAuthorID = pr.User.Id;
			ViewBag.PullRequestTitle = pr.Title;


			if (token == null)
			{
				ViewBag.AuthHref = String.Concat(generalConfiguration.RootURL.ToString(), "Authorize/Login/", number);
				ViewBag.AuthTitle = stringLocalizer["SignIn"];
				ViewBag.IsMaintainer = false;
			}
			else
			{
				var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);
				ViewBag.IsMaintainer = await gitHubManager.UserHasWriteAccess(user).ConfigureAwait(false);
				ViewBag.AuthHref = String.Concat(generalConfiguration.RootURL.ToString(), "Authorize/SignOut/", number);
				ViewBag.AuthTitle = stringLocalizer["SignOut", user.Login];
			}
			return View();
		}
	}
}
