namespace TGWebhooks.Api
{
	/// <summary>
	/// Represents the git repository being manager by the <see cref="TGWebhooks"/>
	/// </summary>
    public interface IRepository : IInitializable
    {
		/// <summary>
		/// The path to the <see cref="IRepository"/>
		/// </summary>
		string Path { get; }
    }
}
