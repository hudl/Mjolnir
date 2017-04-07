using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using System;

namespace Hudl.Mjolnir.Metrics
{
    internal interface IStandardCommandMetricsConfig
    {
        long GetWindowMillis(GroupKey key);
        long GetSnapshotTtlMillis(GroupKey key);
    }

    internal class StandardCommandMetricsConfig : IStandardCommandMetricsConfig
    {
        private readonly IMjolnirConfig _config;

        public StandardCommandMetricsConfig(IMjolnirConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// The length of time to accumulate metrics before resetting.
        /// </summary>
        public long GetWindowMillis(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.metrics.{key}.windowMillis", null) ?? _config.GetConfig<long>("mjolnir.metrics.default.windowMillis", 30000);
        }

        public long GetSnapshotTtlMillis(GroupKey key)
        {
            return _config.GetConfig<long?>($"mjolnir.metrics.{key}.snapshotTtlMillis", null) ?? _config.GetConfig<long>("mjolnir.metrics.default.snapshotTtlMillis", 1000);
        }
    }
}
