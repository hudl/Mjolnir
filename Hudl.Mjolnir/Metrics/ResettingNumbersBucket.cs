using System;
using System.Threading;
using Hudl.Common.Clock;
using log4net;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Metrics
{
    /// <summary>
    /// Keeps track of a collection of values for a configured window of time.
    /// When the window passes, the values are reset.
    /// </summary>
    internal class ResettingNumbersBucket
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ResettingNumbersBucket));

        private readonly IStandardCommandMetricsConfig _config;
        private readonly IClock _clock;
        private readonly GroupKey _key;
        private readonly object _resetBucketLock = new { };

        private ILongCounter[] _counters;
        private long _lastResetAtTime = 0;

        internal ResettingNumbersBucket(GroupKey key, IStandardCommandMetricsConfig config) : this(key, new SystemClock(), config)
        { }

        internal ResettingNumbersBucket(GroupKey key, IClock clock, IStandardCommandMetricsConfig config)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _key = key;
            _clock = clock;
            _config = config;

            _counters = CreateCounters();
            _lastResetAtTime = clock.GetMillisecondTimestamp();
        }

        internal void Increment(CounterMetric metric)
        {
            // Notes:
            // - If we have long periods with no stats, we won't reset until we get one.
            // - This is a "pretty close" implementation.
            //   - Accuracy on Gets isn't 100% and is subject to racing.
            //   - We may write metrics into "old" buckets immediately before resetting at the interval.

            if (_clock.GetMillisecondTimestamp() - _lastResetAtTime > _config.GetWindowMillis(_key))
            {
                Reset();
            }
           
            // See note in Reset() about potential for losing current window counts here. 

            _counters[(int) metric].Increment();
        }

        private ILongCounter[] CreateCounters()
        {
            var values = Enum.GetValues(typeof(CounterMetric));
            var counters = new ILongCounter[values.Length];
            foreach (var value in values)
            {
                counters[(int)value] = new InterlockingLongCounter();
            }
            return counters;
        }

        internal long GetCount(CounterMetric metric)
        {
            return _counters[(int) metric].Get();
        }

        internal void Reset()
        {
            if (!Monitor.TryEnter(_resetBucketLock))
            {
                // Another thread already requested Reset(), we'll let them take care of it.

                // If we were supposed to Reset(), but this thread couldn't acquire the lock,
                // we may end up incrementing counts in the previous bucket before the other
                // thread reassigns _counters. For now, I think that's okay.

                // One way to combat this would be to keep an intermediate "carryover"
                // counter that gets incremented in this block.

                // Subsequent calls to Increment() could look at the carryover and, if > 0
                // add those to the current bucket. Carryover values would have to expire
                // at the same time the regular bucket periods do to avoid carryover that's
                // followed by a long (> period) gap of no increments.
                return;
            }

            try
            {
                var newBucket = CreateCounters();
                _counters = newBucket;

                // Should be the last statement in the try - see comment in catch block.
                _lastResetAtTime = _clock.GetMillisecondTimestamp();
            }
            catch (Exception e)
            {
                // If there's an exception, _lastResetAtTime won't get updated - the next
                // Increment() will try a Reset() again, which is good. 
                Log.Error("Error resetting metrics", e);
            }
            finally
            {
                Monitor.Exit(_resetBucketLock);
            }
        }
    }
}