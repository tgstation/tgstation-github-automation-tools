using System;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Interface
{
	/// <summary>
	/// Async lock context helper
	/// </summary>
    public sealed class SemaphoreSlimContext : IDisposable
    {
		/// <summary>
		/// Asyncronously locks a <paramref name="semaphore"/>
		/// </summary>
		/// <param name="semaphore">The <see cref="SemaphoreSlim"/> to lock</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="SemaphoreSlimContext"/> for the lock</returns>
		public static async Task<SemaphoreSlimContext> Lock(SemaphoreSlim semaphore, CancellationToken cancellationToken)
		{
			if (semaphore == null)
				throw new ArgumentNullException(nameof(semaphore));
			await semaphore.WaitAsync(cancellationToken);
			cancellationToken.ThrowIfCancellationRequested();
			return new SemaphoreSlimContext(semaphore);
		}

		/// <summary>
		/// The locked <see cref="SemaphoreSlim"/>
		/// </summary>
		SemaphoreSlim lockedSemaphore;

		/// <summary>
		/// Construct a <see cref="SemaphoreSlimContext"/>
		/// </summary>
		/// <param name="_lockedSemaphore">The value of <see cref="lockedSemaphore"/></param>
		SemaphoreSlimContext(SemaphoreSlim _lockedSemaphore)
		{
			lockedSemaphore = _lockedSemaphore;
		}
		
		/// <summary>
		/// Finalize the <see cref="SemaphoreSlimContext"/>
		/// </summary>
		~SemaphoreSlimContext()
		{
			Dispose();
		}

		/// <summary>
		/// Release the lock on <see cref="lockedSemaphore"/>
		/// </summary>
		public void Dispose()
		{
			if (lockedSemaphore == null)
				return;
			lockedSemaphore.Release();
			lockedSemaphore = null;
			GC.SuppressFinalize(this);
		}
	}
}
