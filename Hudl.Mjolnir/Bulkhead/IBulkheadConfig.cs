using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadConfig
    {
        int GetMaxConcurrent(GroupKey key);
    }
}
