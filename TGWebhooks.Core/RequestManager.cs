using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

using StreamReader = System.IO.StreamReader;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
	sealed class RequestManager : IRequestManager
	{
		/// <inheritdoc />
		public async Task<string> RunRequest(string url, IEnumerable<string> headers, RequestMethod requestMethod, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			var request = WebRequest.Create(url);
			request.Method = requestMethod.ToString();
			foreach (var I in headers)
				request.Headers.Add(I);

			var tcs = new TaskCompletionSource<string>();
			using (cancellationToken.Register(() => request.Abort()))
			{
				request.BeginGetResponse(new AsyncCallback(async (r) =>
				{
					if (cancellationToken.IsCancellationRequested)
						tcs.SetCanceled();
					else
						using (var response = request.EndGetResponse(r))
						using (var reader = new StreamReader(response.GetResponseStream()))
							tcs.SetResult(await reader.ReadToEndAsync());
				}), null);

				return await tcs.Task;
			}
		}
	}
}
