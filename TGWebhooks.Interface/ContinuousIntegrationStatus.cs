namespace TGWebhooks.Interface
{
	/// <summary>
	/// Describes the status for a <see cref="IContinuousIntegration"/> provider
	/// </summary>
	public enum ContinuousIntegrationStatus
	{
		/// <summary>
		/// The status for the <see cref="IContinuousIntegration"/> isn't present
		/// </summary>
		NotPresent,
		/// <summary>
		/// The job is pending
		/// </summary>
		Pending,
		/// <summary>
		/// The job has failed
		/// </summary>
		Failed,
		/// <summary>
		/// The job has passed
		/// </summary>
		Passed
	}
}
