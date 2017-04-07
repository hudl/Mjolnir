using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Bulkhead
{
    internal interface IBulkheadConfig
    {
        int GetMaxConcurrent(GroupKey key);

        void AddChangeHandler<T>(GroupKey key, Action<T> onConfigChange);

        // Only really exists so we can log it in a validation error message.
        string GetConfigKey(GroupKey key);
    }
}
