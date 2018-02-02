using Octokit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// <see langword="interface"/> for providing access to webhook components
	/// </summary>
    public interface IComponentProvider
	{
		/// <summary>
		/// The <see cref="IMergeRequirement"/>s the <see cref="IComponentProvider"/> contains
		/// </summary>
		IEnumerable<IMergeRequirement> MergeRequirements { get; }

		/// <summary>
		/// Load components in the <see cref="IComponentProvider"/>. Will be called before any other function
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task LoadComponents(CancellationToken cancellationToken);

		/// <summary>
		/// The <see cref="IPayloadHandler{TPayload}"/>s the <see cref="IComponentProvider"/> contains
		/// </summary>
		IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload;
	}
}
