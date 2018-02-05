using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Api;

using StreamReader = System.IO.StreamReader;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class WebRequestManager : IWebRequestManager
#pragma warning restore CA1812
	{
		/// <inheritdoc />
		public async Task<string> RunRequest(Uri url, string body, IEnumerable<string> headers, RequestMethod requestMethod, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			var request = WebRequest.Create(url);
			request.Method = requestMethod.ToString();
			foreach (var I in headers)
				request.Headers.Add(I);

			using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false)) {
				var data = Encoding.UTF8.GetBytes(body);
				await requestStream.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
			}

			try
			{
				WebResponse response;
				using (cancellationToken.Register(() => request.Abort()))
					response = await request.GetResponseAsync().ConfigureAwait(false);

				using (var reader = new StreamReader(response.GetResponseStream()))
					return await reader.ReadToEndAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				if (cancellationToken.IsCancellationRequested)
					throw new OperationCanceledException("RunRequest() cancelled!", e, cancellationToken);
				throw;
			}
		}
	}
}
