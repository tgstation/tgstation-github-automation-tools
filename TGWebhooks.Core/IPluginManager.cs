using Octokit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages reflected <see cref="IPlugin"/>s
	/// </summary>
	public interface IPluginManager
	{
		/// <summary>
		/// Get all enabled <see cref="IPayloadHandler{TPayload}"/>s for a given <typeparamref name="TPayload"/>
		/// </summary>
		/// <typeparam name="TPayload">The payload type to get handlers for</typeparam>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="List{T}"/> of <see cref="IPayloadHandler{TPayload}"/>s that match the given <typeparamref name="TPayload"/></returns>
		Task<List<IPayloadHandler<TPayload>>> GetActivePayloadHandlers<TPayload>(CancellationToken cancellationToken) where TPayload : ActivityPayload;

		/// <summary>
		/// Get all enabled <see cref="IMergeRequirement"/>s
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="List{T}"/> of <see cref="IMergeRequirement"/>s</returns>
		Task<List<IMergeRequirement>> GetActiveMergeRequirements(CancellationToken cancellationToken);
	}
}
