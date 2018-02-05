using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <inheritdoc />
#pragma warning disable CA1812
	sealed class Logger : ILogger
#pragma warning restore CA1812
	{
		/// <summary>
		/// The directory to store log files in
		/// </summary>
		const string LogDirectory = "Logs";

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="Logger"/>
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// Path to the master log file for the instance
		/// </summary>
		readonly string logFile;

		/// <summary>
		/// Construct a <see cref="Logger"/>
		/// </summary>
		/// <param name="_ioManager">The value of <see cref="ioManager"/></param>
		public Logger(IIOManager _ioManager)
		{
			ioManager = new ResolvingIOManager(_ioManager ?? throw new ArgumentNullException(nameof(_ioManager)), _ioManager.ConcatPath(Application.DataDirectory, LogDirectory));
			logFile = ioManager.ResolvePath(ioManager.ConcatPath(Guid.NewGuid().ToString(), ".txt"));
		}

		/// <inheritdoc />
		public async Task LogUnhandledException(Exception exception, CancellationToken cancellationToken)
		{
			var errorLogMessage = String.Format(CultureInfo.CurrentCulture, "{0}: {1}{2}", DateTime.Now.ToLongTimeString(), exception, Environment.NewLine);
			await ioManager.CreateDirectory(".", cancellationToken).ConfigureAwait(false);
			await ioManager.AppendAllText(logFile, errorLogMessage, cancellationToken).ConfigureAwait(false);
		}
	}
}
