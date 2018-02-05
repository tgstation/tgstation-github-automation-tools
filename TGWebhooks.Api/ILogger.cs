using System;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Logging <see langword="interface"/>
	/// </summary>
    public interface ILogger
    {
		/// <summary>
		/// Log an <see cref="Exception"/> that has no other handler
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> to log</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		Task LogUnhandledException(Exception exception, CancellationToken cancellationToken);
    }
}
