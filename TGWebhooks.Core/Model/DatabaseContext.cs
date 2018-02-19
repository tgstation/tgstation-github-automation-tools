using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace TGWebhooks.Core.Model
{
    public class DatabaseContext : DbContext
    {
		public DbSet<AccessTokenEntry> AccessTokenEntries { get; set; }
    }
}
