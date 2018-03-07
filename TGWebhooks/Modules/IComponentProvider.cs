using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Modules
{
	/// <summary>
	/// <see cref="IComponentProviderBase"/> which requires a <see cref="IRepositoryContext"/>
	/// </summary>
    public interface IComponentProvider : IComponentProviderBase
    {
		/// <summary>
		/// Loads the components for a given <paramref name="repositoryId"/>. The returned <see cref="IRepositoryContext"/> must be active before calling any functions in the <see cref="IComponentProviderBase"/>
		/// </summary>
		/// <param name="repositoryId">The <see cref="Octokit.Repository.Id"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="IRepositoryContext"/></returns>
		Task<IRepositoryContext> UsingRepositoryId(long repositoryId, CancellationToken cancellationToken);
    }
}
