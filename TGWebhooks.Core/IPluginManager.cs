using Octokit;
using System.Collections.Generic;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages reflected <see cref="IPlugin"/>s
	/// </summary>
	interface IPluginManager
	{
		/// <summary>
		/// Get all enabled <see cref="IPayloadHandler{TPayload}"/> for a given <typeparamref name="TPayload"/>
		/// </summary>
		/// <typeparam name="TPayload">The payload type to get handlers for</typeparam>
		/// <returns>An <see cref="IEnumerable{T}"/> of <see cref="IPayloadHandler{TPayload}"/> that match the given <typeparamref name="TPayload"/></returns>
		IEnumerable<IPayloadHandler<TPayload>> GetActivePayloadHandlers<TPayload>() where TPayload : ActivityPayload;
	}
}
