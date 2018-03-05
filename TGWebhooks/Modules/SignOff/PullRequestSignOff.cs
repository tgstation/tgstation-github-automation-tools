using System.Collections.Generic;

namespace TGWebhooks.Modules.SignOff
{
	/// <summary>
	/// Represents sign offs for a <see cref="Octokit.PullRequest"/>
	/// </summary>
#pragma warning disable CA1812
	sealed class PullRequestSignOff
#pragma warning restore CA1812
	{
		public string AccessToken { get; set; }
	}
}
