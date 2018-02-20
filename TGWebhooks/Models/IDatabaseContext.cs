using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace TGWebhooks.Models
{
    interface IDatabaseContext
	{
		DbSet<AccessTokenEntry> AccessTokenEntries { get; set; }
		DbSet<KeyValuePair> KeyValuePairs { get; set; }
		DbSet<ModuleMetadata> ModuleMetadatas { get; set; }

		Task Save(CancellationToken cancellationToken);
	}
}
