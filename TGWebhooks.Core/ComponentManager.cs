using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using TGWebhooks.Core.Model;
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
		/// The <see cref="IRootDataStore"/> for the <see cref="ComponentManager"/>
		/// </summary>
		readonly IRootDataStore rootDataStore;

		/// <summary>
		/// Construct a <see cref="ComponentManager"/>
		/// </summary>
		/// <param name="_pluginManager">The value of <see cref="pluginManager"/></param>
		/// <param name="_repository">The value of <see cref="repository"/></param>
		/// <param name="_rootDataStore">The value of <see cref="rootDataStore"/></param>
		public ComponentManager(IPluginManager _pluginManager, IRepository _repository, IRootDataStore _rootDataStore)
		{
			pluginManager = _pluginManager ?? throw new ArgumentNullException(nameof(_pluginManager));
			repository = _repository ?? throw new ArgumentNullException(nameof(_repository));
			rootDataStore = _rootDataStore ?? throw new ArgumentNullException(nameof(_rootDataStore));
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
				repository.Initialize(cancellationToken),
				rootDataStore.Initialize(cancellationToken)
				);
		}
	}
}
