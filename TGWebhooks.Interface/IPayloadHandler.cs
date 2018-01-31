using Octokit;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Handles a certain type of <see cref="ActivityPayload"/>. Constructed via reflection, and accept parameters of <see cref="IGitHubManager"/>, <see cref="IRepository"/>, and <see cref="ILogger"/> in any order
	/// </summary>
	/// <typeparam name="TPayload">The <see cref="ActivityPayload"/> being handled</typeparam>
    public interface IPayloadHandler<TPayload> : IPlugin where TPayload : ActivityPayload
    {
		/// <summary>
		/// Process a <typeparamref name="TPayload"/>
		/// </summary>
		/// <param name="payload">The <typeparamref name="TPayload"/> being processed</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ProcessPayload(TPayload payload);
    }
}
