using System;
using System.Diagnostics;
using System.Threading;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;
using Hudl.Riemann;

namespace Hudl.Mjolnir.Metrics
{
    internal class StandardCommandMetrics : ICommandMetrics
    {
        private readonly IClock _clock;
        private readonly ResettingNumbersBucket _resettingNumbersBucket;
        private readonly IConfigurableValue<long> _snapshotTtlMillis;
        private readonly GroupKey _key;
        private readonly IRiemann _riemann;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        public StandardCommandMetrics(GroupKey key, IConfigurableValue<long> windowMillis, IConfigurableValue<long> snapshotTtlMillis)
            : this(key, windowMillis, snapshotTtlMillis, new SystemClock()) {}

        internal StandardCommandMetrics(GroupKey key, IConfigurableValue<long> windowMillis, IConfigurableValue<long> snapshotTtlMillis, IClock clock, IRiemann riemann = null)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            _key = key;
            _clock = clock;
            _snapshotTtlMillis = snapshotTtlMillis;
            _resettingNumbersBucket = new ResettingNumbersBucket(_clock, windowMillis);
            _riemann = (riemann ?? RiemannStats.Instance);

            _timer = new GaugeTimer((source, args) =>
            {
                // If our stats are sparse, _lastSnapshot might be fairly stale, and not reflective of
                // the actual state of the metrics/bucket. Actually grab a snapshot here (which will
                // rebuild the cached _lastSnapshot if its TTL has expired).

                // Note that it's helpful if the Timer is on an interval that's a bit larger than the 
                // snapshot's TTL to avoid flapping, and also to avoid rather unnecessary snapshot
                // rebuilds.

                _riemann.ConfigGauge(RiemannPrefix + " conf.windowMillis", windowMillis.Value);
                _riemann.ConfigGauge(RiemannPrefix + " conf.snapshotTtlMillis", _snapshotTtlMillis.Value);
            });
        }

        private string RiemannPrefix
        {
            get { return "mjolnir metrics " + _key; }
        }

        public void MarkCommandSuccess()
        {
            _riemann.Event(RiemannPrefix + " Mark", CounterMetric.CommandSuccess.ToString(), null);
            _resettingNumbersBucket.Increment(CounterMetric.CommandSuccess);
        }

        public void MarkCommandFailure()
        {
            _riemann.Event(RiemannPrefix + " Mark", CounterMetric.CommandFailure.ToString(), null);
            _resettingNumbersBucket.Increment(CounterMetric.CommandFailure);
        }

        private long _lastSnapshotTimestamp = 0;
        private MetricsSnapshot _lastSnapshot = new MetricsSnapshot(0, 0);

        public MetricsSnapshot GetSnapshot()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var lastSnapshotTime = _lastSnapshotTimestamp;
                var currentTime = _clock.GetMillisecondTimestamp();

                if (_lastSnapshot == null || currentTime - lastSnapshotTime > _snapshotTtlMillis.Value)
                {
                    // Try to update the _lastSnapshotTimestamp. If we update it, this thread will take on the authority of updating
                    // the snapshot. CompareExchange returns the original result, so if it's different from currentTime, we successfully exchanged.
                    if (Interlocked.CompareExchange(ref _lastSnapshotTimestamp, currentTime, _lastSnapshotTimestamp) != currentTime)
                    {
                        var createwatch = Stopwatch.StartNew();
                        try
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
                        finally
                        {
                            createwatch.Stop();
                            _riemann.Elapsed(RiemannPrefix + " CreateSnapshot", null, createwatch.Elapsed);
                        }
                    }
                }

                return _lastSnapshot;
            }
            finally
            {
                _riemann.Elapsed(RiemannPrefix + " GetSnapshot", null, stopwatch.Elapsed);
            }
        }

        public void Reset()
        {
            // TODO For stats purposes on sparse operations, it might be nice to have this called
            // at regular intervals if we can.
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _resettingNumbersBucket.Reset();
            }
            finally
            {
                _riemann.Elapsed(RiemannPrefix + " Reset", null, stopwatch.Elapsed);
            }
        }
    }
}