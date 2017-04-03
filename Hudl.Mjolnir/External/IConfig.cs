using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.External
{
    public interface IConfig
    {
        T GetConfig<T>(string key, T defaultValue);
        
        // TODO untested interface / implementation. change handler firing needs some more scrutiny.
        // TODO config implementation needs to ensure that change handlers don't get GC'ed

        void AddChangeHandler<T>(string key, Action<T> onConfigChange);
    }

    // TODO split out the implementations and internal classes and move them over to a `Config` directory (i.e. not in External)

    internal class DefaultValueConfig : IConfig
    {
        public T GetConfig<T>(string key, T defaultValue)
        {
            return defaultValue;
        }
        
        public void AddChangeHandler<T>(string key, Action<T> onConfigChange)
        {
            // No-op for default value config.
        }
    }

    // TODO note about implementation needing to cache dynamic/generated values (e.g. mjolnir.command.{name}.Timeout)

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
        private readonly IConfig _config;

        public FailurePercentageCircuitBreakerConfig(IConfig config)
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

    internal interface IStandardCommandMetricsConfig
    {
        long GetWindowMillis(GroupKey key);
        long GetSnapshotTtlMillis(GroupKey key);
    }

    internal class StandardCommandMetricsConfig : IStandardCommandMetricsConfig
    {
        private readonly IConfig _config;

        public StandardCommandMetricsConfig(IConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }
        
        /// <summary>
        /// The length of time to accumulate metrics before resetting.
        /// </summary>
        public long GetWindowMillis(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<long?>($"mjolnir.metrics.{key}.windowMillis", null) ?? _config.GetConfig<long>("mjolnir.metrics.default.windowMillis", 30000);
        }
        
        public long GetSnapshotTtlMillis(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<long?>($"mjolnir.metrics.{key}.snapshotTtlMillis", null) ?? _config.GetConfig<long>("mjolnir.metrics.default.snapshotTtlMillis", 1000);
        }
    }

    internal interface IBulkheadConfig
    {
        int GetMaxConcurrent(GroupKey key);

        void AddChangeHandler<T>(GroupKey key, Action<T> onConfigChange);

        // Only really exists so we can log it in a validation error message.
        string GetConfigKey(GroupKey key);
    }

    internal class BulkheadConfig : IBulkheadConfig
    {
        private readonly IConfig _config;

        public BulkheadConfig(IConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
        }
        
        public int GetMaxConcurrent(GroupKey key)
        {
            // TODO verify null is returned by config impl if not set
            return _config.GetConfig<int?>(GetConfigKey(key), null) ?? _config.GetConfig<int>("mjolnir.bulkhead.default.maxConcurrent", 10);
        }
        
        public void AddChangeHandler<T>(GroupKey key, Action<T> onConfigChange)
        {
            _config.AddChangeHandler(GetConfigKey(key), onConfigChange);
        }

        public string GetConfigKey(GroupKey key)
        {
            return $"mjolnir.bulkhead.{key}.maxConcurrent";
        }
    }
}
