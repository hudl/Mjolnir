using System;
using System.Threading;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Clock;

namespace Hudl.Mjolnir.Metrics
{
    internal class StandardCommandMetrics : ICommandMetrics
    {
        private readonly IClock _clock;
        private readonly ResettingNumbersBucket _resettingNumbersBucket;
        private readonly GroupKey _key;
        private readonly IStandardCommandMetricsConfig _config;

        internal StandardCommandMetrics(GroupKey key, IStandardCommandMetricsConfig config)
            : this(key, config, new UtcSystemClock()) {}

        internal StandardCommandMetrics(GroupKey key, IStandardCommandMetricsConfig config, IClock clock)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _key = key;
            _config = config;
            _clock = clock;
            _resettingNumbersBucket = new ResettingNumbersBucket(_key, _clock, _config);
        }
        
        public void MarkCommandSuccess()
        {
            _resettingNumbersBucket.Increment(CounterMetric.CommandSuccess);
        }

        public void MarkCommandFailure()
        {
            _resettingNumbersBucket.Increment(CounterMetric.CommandFailure);
        }

        private long _lastSnapshotTimestamp = 0;
        private MetricsSnapshot _lastSnapshot = new MetricsSnapshot(0, 0);

        public MetricsSnapshot GetSnapshot()
        {
            var lastSnapshotTime = _lastSnapshotTimestamp;
            var currentTime = _clock.GetMillisecondTimestamp();

            if (_lastSnapshot == null || currentTime - lastSnapshotTime > _config.GetSnapshotTtlMillis(_key))
            {
                // Try to update the _lastSnapshotTimestamp. If we update it, this thread will take on the authority of updating
                // the snapshot. CompareExchange returns the original result, so if it's different from currentTime, we successfully exchanged.
                if (Interlocked.CompareExchange(ref _lastSnapshotTimestamp, currentTime, _lastSnapshotTimestamp) != currentTime)
                {
                    // TODO rob.hruska 11/8/2013 - May be inaccurate if counts are incremented as we're querying these.
                    var success = _resettingNumbersBucket.GetCount(CounterMetric.CommandSuccess);
                    var failure = _resettingNumbersBucket.GetCount(CounterMetric.CommandFailure);
                    var total = success + failure;

                    int errorPercentage;
                    if (total == 0)
                    {
                        errorPercentage = 0;
                    }
                    else
                    {
                        errorPercentage = (int) (success == 0 ? 100 : (failure / (double) total) * 100);
                    }

                    _lastSnapshot = new MetricsSnapshot(total, errorPercentage);
                }
            }

            return _lastSnapshot;
        }

        public void Reset()
        {
            _resettingNumbersBucket.Reset();
        }
    }
}