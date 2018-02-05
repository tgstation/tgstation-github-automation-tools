using System.Collections.Generic;

namespace TGWebhooks.Api
{
	/// <summary>
	/// Represents the requirements for auto merging a <see cref="Octokit.PullRequest"/>
	/// </summary>
	public sealed class AutoMergeStatus
	{
		/// <summary>
		/// How many requirements have been fufilled
		/// </summary>
		public int Progress { get; set; }
		/// <summary>
		/// How many requirements are required for an automatic merge
		/// </summary>
		public int RequiredProgress { get; set; }
		/// <summary>
		/// The delay in seconds until the <see cref="IMergeRequirement"/> should be reevaluated for the <see cref="Octokit.PullRequest"/>
		/// </summary>
		public int ReevaluateIn { get; set; }
		/// <summary>
		/// Lines to display under the progress indicator of the <see cref="AutoMergeStatus"/>. Should be 
		/// </summary>
		public List<string> Notes { get; } = new List<string>();
		/// <summary>
		/// The GitHub access token override to merge with
		/// </summary>
		public string MergerAccessToken { get; }
	}
}
