using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Represents an object that requires initialization before use
	/// </summary>
    public interface IInitializable
	{
		/// <summary>
		/// Load components in the <see cref="IComponentProvider"/>. Will be called before any other function. May be called multiple times
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Initialize(CancellationToken cancellationToken);
	}
}
