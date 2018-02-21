using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules.GameAnnouncer
{
	/// <summary>
	/// Implements the game announcer <see cref="IModule"/>
	/// </summary>
	public sealed class GameAnnouncerModule : IModule, IPayloadHandler<PullRequestEventPayload>
	{
		/// <inheritdoc />
		public Guid Uid => new Guid("a52b2545-94e3-4d74-bb8a-bb9ca94194c3");

		/// <inheritdoc />
		public string Name => "Game Announcer";

		/// <inheritdoc />
		public string Description => "Announces pull request events to game servers";

		/// <inheritdoc />
		public IEnumerable<IMergeRequirement> MergeRequirements => Enumerable.Empty<IMergeRequirement>();

		/// <inheritdoc />
		public IEnumerable<IMergeHook> MergeHooks => Enumerable.Empty<IMergeHook>();

		/// <inheritdoc />
		public IEnumerable<IPayloadHandler<TPayload>> GetPayloadHandlers<TPayload>() where TPayload : ActivityPayload
		{
			if (typeof(TPayload) == typeof(PullRequestEventPayload))
				yield return (IPayloadHandler<TPayload>)(object)this;
		}

		/// <inheritdoc />
		public Task Initialize(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <inheritdoc />
		public Task ProcessPayload(PullRequestEventPayload payload, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
