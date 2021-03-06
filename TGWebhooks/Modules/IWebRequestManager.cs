﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// <see langword="interface"/> for making web requests
	/// </summary>
	public interface IWebRequestManager
	{
		/// <summary>
		/// Make a web request
		/// </summary>
		/// <param name="url">The <see cref="Uri"/> of the request</param>
		/// <param name="body">The body of the request</param>
		/// <param name="headers">HTTP headers of the request</param>
		/// <param name="requestMethod">The <see cref="RequestMethod"/> for the request</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the body of the response of the request on success, <see langword="null"/> on failure</returns>
		Task<string> RunRequest(Uri url, string body, IEnumerable<string> headers, RequestMethod requestMethod, CancellationToken cancellationToken);
	}
}
