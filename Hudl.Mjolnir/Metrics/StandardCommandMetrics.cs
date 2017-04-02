using System;
using System.Threading;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Metrics
{
    internal class StandardCommandMetrics : ICommandMetrics
    {
        private readonly IClock _clock;
        private readonly ResettingNumbersBucket _resettingNumbersBucket;
        private readonly IConfigurableValue<long> _snapshotTtlMillis;
        private readonly GroupKey _key;

        internal StandardCommandMetrics(GroupKey key, IConfigurableValue<long> windowMillis, IConfigurableValue<long> snapshotTtlMillis)
            : this(key, windowMillis, snapshotTtlMillis, new SystemClock()) {}

        internal StandardCommandMetrics(GroupKey key, IConfigurableValue<long> windowMillis, IConfigurableValue<long> snapshotTtlMillis, IClock clock)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            _key = key;
            _clock = clock;
            _snapshotTtlMillis = snapshotTtlMillis;
            _resettingNumbersBucket = new ResettingNumbersBucket(_clock, windowMillis);
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

            if (_lastSnapshot == null || currentTime - lastSnapshotTime > _snapshotTtlMillis.Value)
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