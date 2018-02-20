using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Models
{
	/// <summary>
	/// Represents a database containing models
	/// </summary>
    interface IDatabaseContext
	{
		/// <summary>
		/// The <see cref="AccessTokenEntry"/>s in the database
		/// </summary>
		DbSet<AccessTokenEntry> AccessTokenEntries { get; set; }
		/// <summary>
		/// The <see cref="KeyValuePair"/>s in the database
		/// </summary>
		DbSet<KeyValuePair> KeyValuePairs { get; set; }
		/// <summary>
		/// The <see cref="ModuleMetadata"/>s in the database
		/// </summary>
		DbSet<ModuleMetadata> ModuleMetadatas { get; set; }

		/// <summary>
		/// Save changes to the <see cref="IDatabaseContext"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Save(CancellationToken cancellationToken);
	}
}
