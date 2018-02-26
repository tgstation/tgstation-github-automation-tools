using System.Diagnostics;

namespace Octokit
{
	/// <summary>
	/// Hack until Octokit merges the thing https://github.com/octokit/octokit.net/pull/1767
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	public class PullRequestReviewEventPayload : ActivityPayload
	{
		/// <summary>
		/// 
		/// </summary>
		public string Action { get; protected set; }
		/// <summary>
		/// 
		/// </summary>
		public PullRequest PullRequest { get; protected set; }
		/// <summary>
		/// 
		/// </summary>
		public PullRequestReview Review { get; protected set; }
	}
}
