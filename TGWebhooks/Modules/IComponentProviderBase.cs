using Octokit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// <see langword="interface"/> for providing access to webhook components
	/// </summary>
	public interface IComponentProviderBase
	{
		/// <summary>
		/// The <see cref="IMergeRequirement"/>s the <see cref="IComponentProviderBase"/> contains
		/// </summary>
		IEnumerable<IMergeRequirement> MergeRequirements { get; }

		/// <summary>
		/// The <see cref="IPayloadHandler{TPayload}"/>s the <see cref="IComponentProviderBase"/> contains
		/// </summary>
		IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload;

		/// <summary>
		/// Adds vars to the <see cref="Microsoft.AspNetCore.Mvc.Controller.ViewBag"/> of the <see cref="Controllers.PullRequestController"/>
		/// </summary>
		/// <param name="pullRequest">The <see cref="PullRequest"/> being reviewed</param>
		/// <param name="viewBag">The <see cref="Microsoft.AspNetCore.Mvc.Controller.ViewBag"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken);
	}
}
