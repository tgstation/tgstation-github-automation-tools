using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;
using TGWebhooks.Core;
using TGWebhooks.Models;
using TGWebhooks.Modules;

namespace TGWebhooks.Controllers
{
	/// <summary>
	/// Controller for managing <see cref="IModule"/>s
	/// </summary>
	[Route(Route)]
    public sealed class ModulesController : Controller
    {
		/// <summary>
		/// The route for the <see cref="ModulesController"/>
		/// </summary>
		public const string Route = "Modules";

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="ModulesController"/>
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ModulesController"/>
		/// </summary>
		readonly ILogger<ModulesController> logger;
		/// <summary>
		/// The <see cref="IStringLocalizer"/> for the <see cref="ModulesController"/>
		/// </summary>
		readonly IStringLocalizer<ModulesController> stringLocalizer;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="ModulesController"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;
		/// <summary>
		/// The <see cref="IModuleManager"/> for the <see cref="ModulesController"/>
		/// </summary>
		readonly IModuleManager moduleManager;

		/// <summary>
		/// Construct a <see cref="ModulesController"/>
		/// </summary>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="generalConfiguration"/></param>
		/// <param name="logger">The value of <see cref="logger"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="gitHubManager">The value of <see cref="gitHubManager"/></param>
		/// <param name="moduleManager">The value of <see cref="moduleManager"/></param>
		public ModulesController(IOptions<GeneralConfiguration> generalConfigurationOptions, ILogger<ModulesController> logger, IStringLocalizer<ModulesController> stringLocalizer, IGitHubManager gitHubManager, IModuleManager moduleManager)
		{
			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			this.gitHubManager = gitHubManager ?? throw new ArgumentNullException(nameof(gitHubManager));
			this.moduleManager = moduleManager ?? throw new ArgumentNullException(nameof(moduleManager));
		}

		/// <summary>
		/// Handle a HTTP GET to the <see cref="ModulesController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the action</returns>
		[HttpGet]
		public async Task<IActionResult> Index(CancellationToken cancellationToken)
		{
			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();

			var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(user).ConfigureAwait(false))
				return Forbid();

			ViewBag.Title = stringLocalizer["Title"];
			ViewBag.AuthHref = String.Concat(generalConfiguration.RootURL.ToString(), "Authorize/SignOut");
			ViewBag.AuthTitle = stringLocalizer["SignOut", user.Login];
			ViewBag.ModulesMap = moduleManager.ModuleStatuses;
			return View();
		}

		/// <summary>
		/// Handle a HTTP POST to the <see cref="ModulesController"/>
		/// </summary>
		/// <param name="moduleUpdate">The <see cref="ModuleUpdate"/> to run</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the action</returns>
		[HttpPost]
		public async Task<IActionResult> Update([FromBody] ModuleUpdate moduleUpdate, CancellationToken cancellationToken)
		{
			if (moduleUpdate == null)
				throw new ArgumentNullException(nameof(moduleUpdate));

			var token = await gitHubManager.CheckAuthorization(Request.Cookies, cancellationToken).ConfigureAwait(false);

			if (token == null)
				return Unauthorized();

			var user = await gitHubManager.GetUserLogin(token, cancellationToken).ConfigureAwait(false);

			if (!await gitHubManager.UserHasWriteAccess(user).ConfigureAwait(false))
				return Forbid();

			try
			{
				await moduleManager.SetModuleEnabled(new Guid(moduleUpdate.Uid), moduleUpdate.Enabled, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				logger.LogWarning(e, "Failed to set module {0} enabled to {1}!", moduleUpdate.Uid, moduleUpdate.Enabled);
				return NotFound();
			}

			return Ok();
		}
	}
}