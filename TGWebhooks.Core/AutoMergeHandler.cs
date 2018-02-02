using Hangfire;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Manages the automatic merge process for <see cref="PullRequest"/>s
	/// </summary>
	sealed class AutoMergeHandler : IComponentProvider
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <summary>
		/// The <see cref="IComponentProvider"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IComponentProvider componentProvider;
		/// <summary>
		/// The <see cref="IGitHubManager"/> for the <see cref="AutoMergeHandler"/>
		/// </summary>
		readonly IGitHubManager gitHubManager;

		/// <summary>
		/// Construct an <see cref="AutoMergeHandler"/>
		/// </summary>
		/// <param name="_componentProvider">The value of <see cref="componentProvider"/></param>
		/// <param name="_gitHubManager">The valuse of <see cref="gitHubManager"/></param>
		public AutoMergeHandler(IComponentProvider _componentProvider, IGitHubManager _gitHubManager)
		{
			componentProvider = _componentProvider ?? throw new ArgumentNullException(nameof(_componentProvider));
			gitHubManager = _gitHubManager ?? throw new ArgumentNullException(nameof(_gitHubManager));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
