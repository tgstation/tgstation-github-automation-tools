using Octokit;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Handles a certain type of <see cref="ActivityPayload"/>. Constructed via reflection, and accept parameters of <see cref="IGitHubManager"/>, <see cref="IRepository"/>, and <see cref="ILogger"/> in any order
	/// </summary>
	/// <typeparam name="TPayload">The <see cref="ActivityPayload"/> being handled</typeparam>
    public interface IPayloadHandler<TPayload> where TPayload : ActivityPayload
    {
		/// <summary>
		/// Process a <typeparamref name="TPayload"/>
		/// </summary>
		/// <param name="payload">The <typeparamref name="TPayload"/> being processed</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task ProcessPayload(TPayload payload, CancellationToken cancellationToken);
    }
}
