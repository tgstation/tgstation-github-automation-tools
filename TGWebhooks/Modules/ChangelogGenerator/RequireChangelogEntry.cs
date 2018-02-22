namespace TGWebhooks.Modules.ChangelogGenerator
{
	/// <summary>
	/// Indicates if a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
	/// </summary>
    sealed class RequireChangelogEntry
    {
		/// <summary>
		/// If a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		public bool? Required { get; set; }
    }
}
