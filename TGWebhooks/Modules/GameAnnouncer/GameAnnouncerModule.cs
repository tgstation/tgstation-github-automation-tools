using Byond.TopicSender;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TGWebhooks.Configuration;

namespace TGWebhooks.Modules.GameAnnouncer
{
	/// <summary>
	/// Implements the game announcer <see cref="IModule"/>
	/// </summary>
	sealed class GameAnnouncerModule : IModule, IPayloadHandler<PullRequestEventPayload>
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

		/// <summary>
		/// The <see cref="IByondTopicSender"/> for the <see cref="GameAnnouncerModule"/>
		/// </summary>
		readonly IByondTopicSender byondTopicSender;
		/// <summary>
		/// The <see cref="ServerEntry"/>s for the <see cref="GameAnnouncerModule"/>
		/// </summary>
		readonly ServerConfiguration serverConfiguration;
		/// <summary>
		/// The <see cref="IStringLocalizer"/>s for the <see cref="GameAnnouncerModule"/>
		/// </summary>
		readonly IStringLocalizer<GameAnnouncerModule> stringLocalizer;

		public GameAnnouncerModule(IByondTopicSender byondTopicSender, IOptions<ServerConfiguration> serverConfigurationOptions, IStringLocalizer<GameAnnouncerModule> stringLocalizer)
		{
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			serverConfiguration = serverConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(serverConfigurationOptions));

			byondTopicSender.SendTimeout = serverConfiguration.SendTimeout;
			byondTopicSender.ReceiveTimeout = serverConfiguration.ReceiveTimeout;
		}

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
			if (payload == null)
				throw new ArgumentNullException(nameof(payload));
			switch (payload.Action)
			{
				case "opened":
				case "closed":
				case "reopened":
					//reserialize it
					//.htmlSpecialChars($payload['sender']['login']).': <a href="'.$payload['pull_request']['html_url'].'">'.htmlSpecialChars('#'.$payload['pull_request']['number'].' '.$payload['pull_request']['user']['login'].' - '.$payload['pull_request']['title']).'</a>';
					var json = new SimpleJsonSerializer().Serialize(payload);
					json = byondTopicSender.SanitizeString(json);
					const string innerAnnouncementFormatter = "#{0} {1} - {2}";

					var announcement = String.Format(CultureInfo.CurrentCulture, innerAnnouncementFormatter, payload.PullRequest.Number, payload.PullRequest.User.Login, payload.PullRequest.Title);
					announcement = HttpUtility.HtmlEncode(announcement);
					var announcmentFormatter = stringLocalizer["AnnouncementFormatter"];// "[{0}] Pull Request {1} by {2}: <a href='{3}'>{4}</a>";
					announcement = String.Format(CultureInfo.CurrentCulture, announcmentFormatter, payload.Repository.FullName, payload.PullRequest.Merged ? stringLocalizer["Merged"] : payload.Action, payload.Sender.Login, payload.PullRequest.HtmlUrl, announcement);
					announcement = byondTopicSender.SanitizeString(announcement);

					var startingQuery = String.Format(CultureInfo.InvariantCulture, "?announce={0}&payload={1}&key=", json, announcement);

					var tasks = new List<Task>();
					foreach (var I in serverConfiguration.Entries)
					{
						var final = startingQuery + byondTopicSender.SanitizeString(I.CommsKey);
						tasks.Add(byondTopicSender.SendTopic(I.Address, I.Port, final, cancellationToken));
					}
					return Task.WhenAll(tasks);
			}
			throw new NotSupportedException();
		}
	}
}
