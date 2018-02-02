using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Octokit;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core.Controllers
{
	/// <summary>
	/// <see cref="Controller"/> used for recieving GitHub webhooks
	/// </summary>
	[Produces("application/json")]
	[Route("Payloads")]
	public sealed class PayloadsController : Controller
	{
		/// <summary>
		/// The <see cref="GitHubConfiguration"/> for the <see cref="PayloadsController"/>
		/// </summary>
		readonly GitHubConfiguration gitHubConfiguration;
		/// <summary>
		/// The <see cref="IPluginManager"/> for the <see cref="PayloadsController"/>
		/// </summary>
		readonly IComponentProvider componentProvider;
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="PayloadsController"/>
		/// </summary>
		readonly ILogger logger;

		/// <summary>
		/// Convert some <paramref name="bytes"/> to a hex string
		/// </summary>
		/// <param name="bytes">The <see cref="byte"/> array to convert</param>
		/// <returns><paramref name="bytes"/> as a hex string</returns>
		static string ToHexString(byte[] bytes)
		{
			var builder = new StringBuilder(bytes.Length * 2);
			foreach (byte b in bytes)
				builder.AppendFormat("{0:x2}", b);
			return builder.ToString();
		}

		/// <summary>
		/// Construct a <see cref="PayloadsController"/>
		/// </summary>
		/// <param name="gitHubConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing value of <see cref="gitHubConfiguration"/></param>
		/// <param name="_logger">The value of <see cref="logger"/></param>
		/// <param name="_componentProvider">The value of <see cref="componentProvider"/></param>
		public PayloadsController(IOptions<GitHubConfiguration> gitHubConfigurationOptions, ILogger _logger, IComponentProvider _componentProvider)
		{
			if(gitHubConfigurationOptions == null)
				throw new ArgumentNullException(nameof(gitHubConfigurationOptions));
			gitHubConfiguration = gitHubConfigurationOptions.Value;
			logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
			componentProvider = _componentProvider ?? throw new ArgumentNullException(nameof(_componentProvider));
		}

		/// <summary>
		/// Check that a <paramref name="payload"/> matches it's <paramref name="signatureWithPrefix"/> for the configured secret
		/// </summary>
		/// <param name="payload">The json payload</param>
		/// <param name="signatureWithPrefix">The SHA1 signature</param>
		/// <returns><see langword="true"/> if the <paramref name="payload"/> matches it's <paramref name="signatureWithPrefix"/> for the configured secret</returns>
		bool CheckPayloadSignature(string payload, string signatureWithPrefix)
		{
			const string Sha1Prefix = "sha1=";
			if (!signatureWithPrefix.StartsWith(Sha1Prefix, StringComparison.OrdinalIgnoreCase))
				return false;
			var signature = signatureWithPrefix.Substring(Sha1Prefix.Length);
			var secret = Encoding.UTF8.GetBytes(gitHubConfiguration.WebhookSecret);
			var payloadBytes = Encoding.UTF8.GetBytes(payload);

			byte[] hash;
			using (var hmSha1 = new HMACSHA1(secret))
				hash = hmSha1.ComputeHash(payloadBytes);

			return ToHexString(hash) == signature;
		}
		
		/// <summary>
		/// Invoke the active <see cref="IPayloadHandler{TPayload}"/> for a given <typeparamref name="TPayload"/>
		/// </summary>
		/// <typeparam name="TPayload">The payload type to invoke</typeparam>
		/// <param name="json">The json <see cref="string"/> of the <typeparamref name="TPayload"/></param>
		/// <returns>A <see cref="Task"/> representing the running handlers</returns>
		async Task InvokeHandlers<TPayload>(TPayload payload, IJobCancellationToken jobCancellationToken) where TPayload : ActivityPayload
		{
			var cancellationToken = jobCancellationToken.ShutdownToken;

			await componentProvider.Initialize(cancellationToken);

			var tasks = new List<Task>();
			foreach (var handler in componentProvider.GetPayloadHandlers<TPayload>()) {
				async Task RunHandler()
				{
					try
					{
						await handler.ProcessPayload(payload);
					}
					//To be expected
					catch (NotSupportedException) { }
					catch (Exception e)
					{
						await logger.LogUnhandledException(e, cancellationToken);
					}
				};
				tasks.Add(RunHandler());
			}

			await Task.WhenAll(tasks);
		}

		/// <summary>
		/// Handle a POST to the <see cref="PayloadsController"/>
		/// </summary>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> of the POST</returns>
		[HttpPost]
		public async Task<IActionResult> Receive(CancellationToken cancellationToken)
		{
			if (!Request.Headers.TryGetValue("X-GitHub-Event", out StringValues eventName)
				|| !Request.Headers.TryGetValue("X-Hub-Signature", out StringValues signature)
				|| !Request.Headers.TryGetValue("X-GitHub-Delivery", out StringValues delivery))
				return BadRequest();

			string json;
			using (var reader = new StreamReader(Request.Body))
				json = await reader.ReadToEndAsync();

			if(!CheckPayloadSignature(json, signature))
				return Unauthorized();

			async Task<IActionResult> StartJob<TPayload>() where TPayload : ActivityPayload
			{
				TPayload payload;
				try
				{
					payload = new SimpleJsonSerializer().Deserialize<TPayload>(json);
				}
				catch (Exception e)
				{
					await logger.LogUnhandledException(e, cancellationToken);
					return BadRequest();
				}

				//ensure the payload is from the configured sender
				if (payload.Repository.Owner.Login != gitHubConfiguration.RepoOwner || payload.Repository.Name != gitHubConfiguration.RepoName)
					return Forbid();
				
				BackgroundJob.Enqueue(() => InvokeHandlers<TPayload>(payload, JobCancellationToken.Null));
				return Ok();
			};
			
			switch (eventName)
			{
				case "pull_request":
					return await StartJob<PullRequestEventPayload>();
				default:
					return Ok();
			}			
		}
	}
}