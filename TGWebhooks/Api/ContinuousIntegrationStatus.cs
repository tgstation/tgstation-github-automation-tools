namespace TGWebhooks.Api
{
#pragma warning disable CA1717 // Only FlagsAttribute enums should have plural names
	/// <summary>
	/// Describes the status for a <see cref="IContinuousIntegration"/> provider
	/// </summary>
	enum ContinuousIntegrationStatus
#pragma warning restore CA1717 // Only FlagsAttribute enums should have plural names
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
