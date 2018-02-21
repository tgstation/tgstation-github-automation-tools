using Octokit;
using System.Collections.Generic;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// <see langword="interface"/> for providing access to webhook components
	/// </summary>
	public interface IComponentProvider : IInitializable
	{
		/// <summary>
		/// The <see cref="IMergeRequirement"/>s the <see cref="IComponentProvider"/> contains
		/// </summary>
		IEnumerable<IMergeRequirement> MergeRequirements { get; }

		/// <summary>
		/// The <see cref="IMergeHook"/>s the <see cref="IComponentProvider"/> contains
		/// </summary>
		IEnumerable<IMergeHook> MergeHooks { get; }

		/// <summary>
		/// The <see cref="IPayloadHandler{TPayload}"/>s the <see cref="IComponentProvider"/> contains
		/// </summary>
		IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload;
	}
}
