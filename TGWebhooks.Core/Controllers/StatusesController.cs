using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TGWebhooks.Core.Controllers
{
	[Route(Route)]
	public class StatusesController : Controller
    {
		public const string Route = "Statues";
    }
}
