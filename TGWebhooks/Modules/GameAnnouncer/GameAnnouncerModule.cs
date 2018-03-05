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
using TGWebhooks.Core;

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
		/// <summary>
		/// The <see cref="IChatMessenger"/> for the <see cref="GameAnnouncerModule"/>
		/// </summary>
		readonly IChatMessenger chatMessenger;

		/// <summary>
		/// Backing field for <see cref="SetEnabled(bool)"/>
		/// </summary>
		bool enabled;

		/// <summary>
		/// Construct a <see cref="GameAnnouncerModule"/>
		/// </summary>
		/// <param name="byondTopicSender">The value of <see cref="byondTopicSender"/></param>
		/// <param name="serverConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="serverConfiguration"/></param>
		/// <param name="stringLocalizer">The value of <see cref="stringLocalizer"/></param>
		/// <param name="chatMessenger">The value of <see cref="chatMessenger"/></param>
		public GameAnnouncerModule(IByondTopicSender byondTopicSender, IOptions<ServerConfiguration> serverConfigurationOptions, IStringLocalizer<GameAnnouncerModule> stringLocalizer, IChatMessenger chatMessenger)
		{
			this.byondTopicSender = byondTopicSender ?? throw new ArgumentNullException(nameof(byondTopicSender));
			serverConfiguration = serverConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(serverConfigurationOptions));
			this.stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
			this.chatMessenger = chatMessenger ?? throw new ArgumentNullException(nameof(chatMessenger));

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
		public Task AddViewVars(PullRequest pullRequest, dynamic viewBag, CancellationToken cancellationToken) => Task.CompletedTask;

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

					var innerAnnouncement = String.Format(CultureInfo.CurrentCulture, innerAnnouncementFormatter, payload.PullRequest.Number, payload.PullRequest.User.Login, payload.PullRequest.Title);
					var announcement = HttpUtility.HtmlEncode(innerAnnouncement);
					var actionString = payload.PullRequest.Merged ? "merged" : payload.Action;
					announcement = stringLocalizer["AnnouncementFormatterGame", payload.Repository.FullName, actionString, payload.Sender.Login, payload.PullRequest.HtmlUrl, announcement];// "[{0}] Pull Request {1} by {2}: <a href='{3}'>{4}</a>";
					announcement = byondTopicSender.SanitizeString(announcement);

					var startingQuery = String.Format(CultureInfo.InvariantCulture, "?announce={0}&payload={1}&key=", json, announcement);

					var chatAnnouncement = stringLocalizer["AnnouncementFormatterChat", payload.Repository.FullName, actionString, payload.Sender.Login, payload.PullRequest.HtmlUrl, innerAnnouncement];

					var tasks = new List<Task>
					{
						//send it to the chats
						chatMessenger.SendMessage(chatAnnouncement, cancellationToken)
					};
					foreach (var I in serverConfiguration.Entries)
					{
						var final = startingQuery + byondTopicSender.SanitizeString(I.CommsKey);
						tasks.Add(byondTopicSender.SendTopic(I.Address, I.Port, final, cancellationToken));
					}
					return Task.WhenAll(tasks);
			}
			throw new NotSupportedException();
		}

		/// <inheritdoc />
		public void SetEnabled(bool enabled) => this.enabled = enabled;
	}
}
