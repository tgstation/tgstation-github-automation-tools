using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Configuration;

namespace TGWebhooks.Core
{
	/// <summary>
	/// <see cref="IChatMessenger"/> for Discord
	/// </summary>
	sealed class DiscordMessenger : IChatMessenger, IDisposable
	{
		/// <summary>
		/// The <see cref="DiscordConfiguration"/> for the <see cref="DiscordMessenger"/>
		/// </summary>
		readonly DiscordConfiguration discordConfiguration;
		/// <summary>
		/// The <see cref="IDiscordClient"/> for the <see cref="DiscordMessenger"/>
		/// </summary>
		readonly DiscordSocketClient discordClient;

		/// <summary>
		/// Construct a <see cref="DiscordMessenger"/>
		/// </summary>
		/// <param name="discordConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="discordConfiguration"/></param>
		public DiscordMessenger(IOptions<DiscordConfiguration> discordConfigurationOptions)
		{
			discordConfiguration = discordConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(discordConfigurationOptions));

			discordClient = new DiscordSocketClient(new DiscordSocketConfig { DefaultRetryMode = RetryMode.AlwaysFail, ConnectionTimeout = discordConfiguration.Timeout });
		}

		/// <inheritdoc />
		public void Dispose() => discordClient.Dispose();

		/// <inheritdoc />
		public async Task SendMessage(string message, CancellationToken cancellationToken)
		{
			if (discordClient.LoginState != LoginState.LoggedIn)
			{
				await discordClient.LoginAsync(TokenType.Bot, discordConfiguration.BotToken).ConfigureAwait(false);
				await discordClient.StartAsync().ConfigureAwait(false);
				var tcs = new TaskCompletionSource<object>();
				discordClient.Ready += () => { tcs.TrySetResult(null); return Task.CompletedTask; };
				using (cancellationToken.Register(() => tcs.SetCanceled()))
					await tcs.Task.ConfigureAwait(false);
			}

			async Task SendDeMessage(SocketTextChannel socketTextChannel)
			{
				try
				{
					await socketTextChannel.SendMessageAsync(message, false, null, new RequestOptions { CancelToken = cancellationToken, Timeout = discordConfiguration.Timeout }).ConfigureAwait(false);
				}
				catch(HttpException e)
				{
					//no perms
					if (e.DiscordCode != 50013)
						throw;
				}
			};

			var user = discordClient.CurrentUser;
			await Task.WhenAll(discordClient.Guilds.SelectMany(x => x.TextChannels).Select(x => SendDeMessage(x))).ConfigureAwait(false);
		}
	}
}
