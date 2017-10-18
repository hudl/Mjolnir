using Hudl.Mjolnir.Key;
using System;
using Hudl.Mjolnir.Config;

namespace Hudl.Mjolnir.Breaker
{
    internal class FailurePercentageCircuitBreakerConfig : IFailurePercentageCircuitBreakerConfig
    {
        private readonly MjolnirConfiguration _config;

        public FailurePercentageCircuitBreakerConfig(MjolnirConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public long GetMinimumOperations(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).MinimumOperations;
        }
        
        public long GetWindowMillis(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).WindowMillis;
        }

        public int GetThresholdPercentage(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).ThresholdPercentage;
        }

        public long GetTrippedDurationMillis(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).TrippedDurationMillis;
        }

        public bool GetForceTripped(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).ForceTripped;
        }

        public bool GetForceFixed(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).ForceFixed;
        }

        public long GetSnapshotTtlMillis(GroupKey key)
        {
            return _config.GetBreakerConfiguration(key.Name).SnapshotTtlMillis;
        }
    }
}
