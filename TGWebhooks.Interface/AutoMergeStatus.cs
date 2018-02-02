using System.Collections.Generic;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Represents the requirements for auto merging a <see cref="Octokit.PullRequest"/>
	/// </summary>
	public sealed class AutoMergeStatus
	{
		/// <summary>
		/// How many requirements have been fufilled
		/// </summary>
		int Progress { get; set; }
		/// <summary>
		/// How many requirements are required for an automatic merge
		/// </summary>
		int RequiredProgress { get; set; }
		/// <summary>
		/// The delay in seconds until the <see cref="IMergeRequirement"/> should be reevaluated for the <see cref="Octokit.PullRequest"/>
		/// </summary>
		int ReevaluateIn { get; set; }
		/// <summary>
		/// Lines to display under the progress indicator of the <see cref="AutoMergeStatus"/>. Should be 
		/// </summary>
		List<string> Notes { get; } = new List<string>();
	}
}
