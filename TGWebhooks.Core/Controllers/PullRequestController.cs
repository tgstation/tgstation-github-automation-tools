using Microsoft.AspNetCore.Mvc;

namespace TGWebhooks.Core.Controllers
{
	/// <summary>
	/// Main <see cref="Octokit.PullRequest"/> management interface
	/// </summary>
	[Route(Route)]
	public class PullRequestController : Controller
    {
		public const string Route = "PullRequest";
    }
}
