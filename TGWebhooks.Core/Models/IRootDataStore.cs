using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TGWebhooks.Interface;

namespace TGWebhooks.Core.Models
{
    interface IRootDataStore : IBranchingDataStore, IInitializable
    {
    }
}
