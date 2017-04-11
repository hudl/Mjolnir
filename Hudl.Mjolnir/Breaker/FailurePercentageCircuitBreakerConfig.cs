using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Breaker
{
    internal class FailurePercentageCircuitBreakerConfig : IFailurePercentageCircuitBreakerConfig
    {
        private readonly IMjolnirConfig _config;

        public FailurePercentageCircuitBreakerConfig(IMjolnirConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public long GetMinimumOperations(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.minimumOperations", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.minimumOperations", 10);
        }
        
        public long GetWindowMillis(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.windowMillis", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.windowMillis", 30000);
        }

        public int GetThresholdPercentage(GroupKey key)
        {
            return _config.GetConfig<int?>($"mjolnir.breaker.{key}.thresholdPercentage", null) ?? _config.GetConfig("mjolnir.breaker.default.thresholdPercentage", 50);
        }

        public long GetTrippedDurationMillis(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.trippedDurationMillis", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.trippedDurationMillis", 10000);
        }

        public bool GetForceTripped(GroupKey key)
        {
            return _config.GetConfig<bool?>($"mjolnir.breaker.{key}.forceTripped", null) ?? _config.GetConfig("mjolnir.breaker.default.forceTripped", false);
        }

        public bool GetForceFixed(GroupKey key)
        {
            return _config.GetConfig<bool?>($"mjolnir.breaker.{key}.forceFixed", null) ?? _config.GetConfig("mjolnir.breaker.default.forceFixed", false);
        }

        public long GetSnapshotTtlMillis(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.snapshotTtlMillis", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.snapshotTtlMillis", 1000);
        }
    }
}
