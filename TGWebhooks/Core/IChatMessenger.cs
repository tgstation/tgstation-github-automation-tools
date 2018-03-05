using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages a chat client
	/// </summary>
	interface IChatMessenger
	{
		/// <summary>
		/// Sends a message to the chat
		/// </summary>
		/// <param name="message">The message to send</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SendMessage(string message, CancellationToken cancellationToken);
	}
}