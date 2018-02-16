using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TGWebhooks.Api;

using StreamReader = System.IO.StreamReader;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class WebRequestManager : IWebRequestManager
#pragma warning restore CA1812
	{
		/// <summary>
		/// The <see cref="ILogger{TCategoryName}"/> for the <see cref="WebRequestManager"/>
		/// </summary>
		readonly ILogger<WebRequestManager> logger;

		/// <summary>
		/// Construct a <see cref="WebRequestManager"/>
		/// </summary>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		public WebRequestManager(ILogger<WebRequestManager> _logger)
		{
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
		}

		/// <inheritdoc />
		public async Task<string> RunRequest(Uri url, string body, IEnumerable<string> headers, RequestMethod requestMethod, CancellationToken cancellationToken)
		{
			if (url == null)
				throw new ArgumentNullException(nameof(url));
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			if (headers == null)
				throw new ArgumentNullException(nameof(headers));

			logger.LogDebug("{0} to {1}", requestMethod, url);
			logger.LogTrace("Body: \"{0}\". Headers: {1}", body, String.Join(';', headers));

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

				string result;
				using (var reader = new StreamReader(response.GetResponseStream()))
					result = await reader.ReadToEndAsync().ConfigureAwait(false);

				logger.LogTrace("Request success.");
				return result;
			}
			catch (Exception e)
			{
				if (cancellationToken.IsCancellationRequested)
					throw new OperationCanceledException("RunRequest() cancelled!", e, cancellationToken);
				logger.LogWarning(e, "Request failed!");
				throw;
			}
		}
	}
}
