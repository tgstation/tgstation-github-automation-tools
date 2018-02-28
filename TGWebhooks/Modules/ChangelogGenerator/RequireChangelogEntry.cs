namespace TGWebhooks.Modules.ChangelogGenerator
{
	/// <summary>
	/// Indicates if a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
	/// </summary>
    sealed class RequireChangelogEntry
    {
		/// <summary>
		/// The person who requested the <see cref="RequireChangelogEntry"/> if any
		/// </summary>
		public string Requestor { get; set; }

		/// <summary>
		/// If a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		public bool? Required { get; set; }
    }
}
