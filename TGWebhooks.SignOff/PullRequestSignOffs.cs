using System.Collections.Generic;

namespace TGWebhooks.SignOff
{
	/// <summary>
	/// Represents sign offs for a <see cref="Octokit.PullRequest"/>
	/// </summary>
	sealed class PullRequestSignOffs
	{
		/// <summary>
		/// <see cref="Dictionary{TKey, TValue}"/> of <see cref="Octokit.PullRequest.Number"/>s <see cref="List{T}"/>s of GitHub logins of signers
		/// </summary>
		public Dictionary<int, List<string>> Entries { get; set; } = new Dictionary<int, List<string>>();
	}
}
