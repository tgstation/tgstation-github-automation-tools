using System;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Logging <see langword="interface"/>
	/// </summary>
    public interface ILogger
    {
		/// <summary>
		/// Log an <see cref="Exception"/> that has no other handler
		/// </summary>
		/// <param name="e">The <see cref="Exception"/> to log</param>
		void LogUnhandledException(Exception e);
    }
}
