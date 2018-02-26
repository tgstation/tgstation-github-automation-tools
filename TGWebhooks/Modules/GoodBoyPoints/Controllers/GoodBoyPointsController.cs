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
		/// Construct a <see cref="GoodBoyPointsController"/>
		/// </summary>
		/// <param name="goodBoyPointsModule">The value of <see cref="goodBoyPointsModule"/></param>
		public GoodBoyPointsController(GoodBoyPointsModule goodBoyPointsModule)
		{
			this.goodBoyPointsModule = goodBoyPointsModule ?? throw new ArgumentNullException(nameof(goodBoyPointsModule));
		}

		/// <summary>
		/// Handle a HTTP GET to the <see cref="GoodBoyPointsController"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="JsonResult"/></returns>
		[HttpGet]
		public async Task<IActionResult> Index(CancellationToken cancellationToken) => Json(await goodBoyPointsModule.GoodBoyPointsEntries(cancellationToken).ConfigureAwait(false));
    }
}