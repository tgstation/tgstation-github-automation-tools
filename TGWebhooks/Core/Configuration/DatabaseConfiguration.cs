namespace TGWebhooks.Core.Configuration
{
	/// <summary>
	/// Database configuration settings
	/// </summary>
	public sealed class DatabaseConfiguration
	{
		/// <summary>
		/// The configuration section the <see cref="DatabaseConfiguration"/> resides in
		/// </summary>
		public const string Section = "Database";
		/// <summary>
		/// The type of the database, case-insensitive. Can be "SQLSERVER", "MYSQL", or "SQLITE"
		/// </summary>
		public string DatabaseType { get; set; }
		/// <summary>
		/// The database connection string
		/// </summary>
		public string ConnectionString { get; set; }
	}
}
