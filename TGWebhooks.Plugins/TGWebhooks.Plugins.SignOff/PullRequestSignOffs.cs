using System.Collections.Generic;

namespace TGWebhooks.Plugins.SignOff
{
	/// <summary>
	/// Represents sign offs for a <see cref="Octokit.PullRequest"/>
	/// </summary>
#pragma warning disable CA1812
	sealed class PullRequestSignOffs
#pragma warning restore CA1812
	{
		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Octokit.PullRequest.Number"/>s <see cref="List{T}"/>s of GitHub logins of signers
		/// </summary>
		public Dictionary<int, List<string>> Entries { get; set; } = new Dictionary<int, List<string>>();
	}
}
