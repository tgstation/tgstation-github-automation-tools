namespace TGWebhooks.Modules
{
	/// <summary>
	/// <see cref="IComponentProviderBase"/> which requires initialization
	/// </summary>
    public interface IComponentProvider : IComponentProviderBase, IInitializable
    {
    }
}
