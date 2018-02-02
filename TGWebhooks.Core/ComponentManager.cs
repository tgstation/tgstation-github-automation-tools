using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TGWebhooks.Interface;

namespace TGWebhooks.Core
{
	/// <summary>
	/// Top level <see cref="IComponentProvider"/>
	/// </summary>
	sealed class ComponentManager : IComponentProvider
	{
		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => throw new NotImplementedException();

		/// <summary>
		/// The <see cref="IPluginManager"/> for the <see cref="ComponentManager"/>
		/// </summary>
		readonly IPluginManager pluginManager;
		/// <summary>
		/// The <see cref="IRepository"/> for the <see cref="ComponentManager"/>
		/// </summary>
		readonly IRepository repository;

		/// <summary>
		/// Construct a <see cref="ComponentManager"/>
		/// </summary>
		/// <param name="_pluginManager">The value of <see cref="pluginManager"/></param>
		public ComponentManager(IPluginManager _pluginManager, IRepository _repository)
		{
			pluginManager = _pluginManager ?? throw new ArgumentNullException(nameof(_pluginManager));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
		}

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			return pluginManager.GetPayloadHandlers<TPayload>();
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken)
		{
			return Task.WhenAll(
				pluginManager.Initialize(cancellationToken),
				repository.Initialize(cancellationToken)
				);
		}
	}
}
