using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadFactory
    {
        ISemaphoreBulkhead GetBulkhead(GroupKey key);
    }
}
