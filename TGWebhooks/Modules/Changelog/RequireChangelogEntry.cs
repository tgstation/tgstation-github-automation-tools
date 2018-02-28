namespace TGWebhooks.Modules.Changelog
{
	/// <summary>
	/// Indicates if a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
	/// </summary>
    public sealed class RequireChangelogEntry
    {
		/// <summary>
		/// If a <see cref="Models.Changelog"/> is required for a <see cref="Octokit.PullRequest"/>
		/// </summary>
		public bool? Required { get; set; }
    }
}
