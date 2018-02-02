using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// <see langword="interface"/> for making web requests
	/// </summary>
    public interface IRequestManager
    {
		/// <summary>
		/// Make a web request
		/// </summary>
		/// <param name="url">The URL of the request</param>
		/// <param name="headers">HTTP headers of the request</param>
		/// <param name="requestMethod">The <see cref="RequestMethod"/> for the request</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the body of the response of the request on success, <see langword="null"/> on failure</returns>
		Task<string> RunRequest(string url, IEnumerable<string> headers, RequestMethod requestMethod, CancellationToken cancellationToken);
    }
}
