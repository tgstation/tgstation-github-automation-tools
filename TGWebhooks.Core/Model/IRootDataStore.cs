using TGWebhooks.Interface;

namespace TGWebhooks.Core.Model
{
    interface IRootDataStore : IBranchingDataStore, IInitializable
    {
    }
}
