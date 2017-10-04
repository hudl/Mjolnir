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

        private BreakerConfiguration _getBreakerConfiguration(GroupKey key)
        {
            BreakerConfiguration breakerConfiguration;
    
            return _config.BreakerConfigurations.TryGetValue(key.Name, out breakerConfiguration) ? 
                breakerConfiguration : 
                _config.DefaultBreakerConfiguration;
        }

        public long GetMinimumOperations(GroupKey key)
        {
            return _getBreakerConfiguration(key).MinimumOperations;
        }
        
        public long GetWindowMillis(GroupKey key)
        {
            return _getBreakerConfiguration(key).WindowMillis;
        }

        public int GetThresholdPercentage(GroupKey key)
        {
            return _getBreakerConfiguration(key).ThresholdPercentage;
        }

        public long GetTrippedDurationMillis(GroupKey key)
        {
            return _getBreakerConfiguration(key).TrippedDurationMillis;
        }

        public bool GetForceTripped(GroupKey key)
        {
            return _getBreakerConfiguration(key).ForceTripped;
        }

        public bool GetForceFixed(GroupKey key)
        {
            return _getBreakerConfiguration(key).ForceFixed;
        }

        public long GetSnapshotTtlMillis(GroupKey key)
        {
            return _getBreakerConfiguration(key).SnapshotTtlMillis;
        }
    }
}
