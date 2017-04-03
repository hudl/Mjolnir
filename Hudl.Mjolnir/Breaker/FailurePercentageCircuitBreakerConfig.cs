using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Breaker
{
    internal interface IFailurePercentageCircuitBreakerConfig
    {
        long GetMinimumOperations(GroupKey key);
        int GetThresholdPercentage(GroupKey key);
        long GetTrippedDurationMillis(GroupKey key);
        bool GetForceTripped(GroupKey key);
        bool GetForceFixed(GroupKey key);
    }

    internal class FailurePercentageCircuitBreakerConfig : IFailurePercentageCircuitBreakerConfig
    {
        private readonly IMjolnirConfig _config;

        public FailurePercentageCircuitBreakerConfig(IMjolnirConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }

        public long GetMinimumOperations(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.minimumOperations", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.minimumOperations", 10);
        }

        public int GetThresholdPercentage(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<int?>($"mjolnir.breaker.{key}.thresholdPercentage", null) ?? _config.GetConfig<int>("mjolnir.breaker.default.thresholdPercentage", 50);
        }

        public long GetTrippedDurationMillis(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<long?>($"mjolnir.breaker.{key}.trippedDurationMillis", null) ?? _config.GetConfig<long>("mjolnir.breaker.default.trippedDurationMillis", 10000);
        }

        public bool GetForceTripped(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<bool?>($"mjolnir.breaker.{key}.forceTripped", null) ?? _config.GetConfig<bool>("mjolnir.breaker.default.forceTripped", false);
        }

        public bool GetForceFixed(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<bool?>($"mjolnir.breaker.{key}.forceFixed", null) ?? _config.GetConfig<bool>("mjolnir.breaker.default.forceFixed", false);
        }
    }
}
