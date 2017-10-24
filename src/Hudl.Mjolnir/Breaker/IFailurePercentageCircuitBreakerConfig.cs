using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Breaker
{
    internal interface IFailurePercentageCircuitBreakerConfig
    {
        long GetMinimumOperations(GroupKey key);
        long GetWindowMillis(GroupKey key);
        int GetThresholdPercentage(GroupKey key);
        long GetTrippedDurationMillis(GroupKey key);
        bool GetForceTripped(GroupKey key);
        bool GetForceFixed(GroupKey key);
        long GetSnapshotTtlMillis(GroupKey key);
    }
}
