using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// Represents an object that requires initialization before use
	/// </summary>
	public interface IInitializable
	{
		/// <summary>
		/// Asyncronously set up the <see cref="IInitializable"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Initialize(CancellationToken cancellationToken);
	}
}
