using System;
using System.Diagnostics;
using System.Threading;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Util;
using log4net;

namespace Hudl.Mjolnir.Breaker
{
    /// <summary>
    /// Trips when the error percentage rises above a configured threshold percentage.
    /// 
    /// This breaker is modeled after Hystrix's HystrixCircuitBreakerImpl. However, the metrics
    /// implementations that drive the breaker are fairly different.
    /// </summary>
    internal class FailurePercentageCircuitBreaker : ICircuitBreaker
    {
        internal readonly FailurePercentageCircuitBreakerProperties Properties;

        private static readonly ILog Log = LogManager.GetLogger(typeof (FailurePercentageCircuitBreaker));

        private readonly object _stateChangeLock = new { };
        private readonly object _singleTestLock = new { };

        private readonly IClock _clock;
        private readonly ICommandMetrics _metrics;

        private readonly GroupKey _key;
        private readonly IStats _stats;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local
        
        private volatile State _state;
        private long _lastTrippedTimestamp;

        internal FailurePercentageCircuitBreaker(GroupKey key, ICommandMetrics metrics, IStats stats, FailurePercentageCircuitBreakerProperties properties)
            : this(key, new SystemClock(), metrics, stats, properties) {}

        internal FailurePercentageCircuitBreaker(GroupKey key, IClock clock, ICommandMetrics metrics, IStats stats, FailurePercentageCircuitBreakerProperties properties, IConfigurableValue<long> gaugeIntervalMillisOverride = null)
        {
            _key = key;
            _clock = clock;
            _metrics = metrics;

            if (stats == null)
            {
                throw new ArgumentNullException("stats");
            }

            _stats = stats;

            Properties = properties;
            _state = State.Fixed; // Start off assuming everything's fixed.
            _lastTrippedTimestamp = 0; // 0 is fine since it'll be far less than the first compared value.

            _timer = new GaugeTimer((source, args) =>
            {
                var snapshot = _metrics.GetSnapshot();
                _stats.Gauge(StatsPrefix + " total", snapshot.Total >= properties.MinimumOperations.Value ? "Above" : "Below", snapshot.Total);
                _stats.Gauge(StatsPrefix + " error", snapshot.ErrorPercentage >= properties.ThresholdPercentage.Value ? "Above" : "Below", snapshot.ErrorPercentage);
            }, gaugeIntervalMillisOverride);
        }

        public ICommandMetrics Metrics
        {
            get { return _metrics; }
        }

        private string StatsPrefix
        {
            get { return "mjolnir breaker " + _key; }
        }

        /// <summary>
        /// Indicates that a recently-completed operation was successful.
        /// 
        /// If the breaker is tripped and <code>MarkSuccess()</code> is called, the breaker will be fixed.
        /// The operation's elapsed duration (in milliseconds) is used to ensure the operation being checked
        /// began before the breaker was initially tripped.
        /// </summary>
        public void MarkSuccess(long elapsedMillis)
        {
            if (_state != State.Tripped || _clock.GetMillisecondTimestamp() - elapsedMillis < _lastTrippedTimestamp)
            {
                // Ignore.
                _stats.Event(StatsPrefix + " MarkSuccess", "Ignored", null);
                return;
            }

            Log.InfoFormat("Fixed Breaker={0}", _key);

            _state = State.Fixed;
            _metrics.Reset();

            _stats.Event(StatsPrefix + " MarkSuccess", "Fixed", null);
        }

        /// <summary>
        /// </summary>
        /// <returns><code>true</code> if this breaker is allowing operations through</returns>
        public bool IsAllowing()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = true;
            try
            {
                if (Properties.ForceTripped.Value)
                {
                    result = false;
                }
                else if (Properties.ForceFixed.Value)
                {
                    // If we're forcing, we still want to keep track of the state in case we remove the force.
                    CheckAndSetTripped();
                    result = true;
                }
                else
                {
                    result = !CheckAndSetTripped() || AllowSingleTest();
                }
            }
            finally
            {
                _stats.Elapsed(StatsPrefix + " IsAllowing", (result ? "Allowed" : "Rejected"), stopwatch.Elapsed);
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns><code>true</code> if we should allow a single test operation through the breaker</returns>
        private bool AllowSingleTest()
        {
            var stopwatch = Stopwatch.StartNew();
            var state = "Unknown";
            try
            {
                if (!Monitor.TryEnter(_singleTestLock))
                {
                    state = "MissedLock";
                    return false;
                }

                try
                {
                    if (_state == State.Tripped && IsPastWaitDuration())
                    {
                        _lastTrippedTimestamp = _clock.GetMillisecondTimestamp();
                        Log.InfoFormat("Allowing single test operation Breaker={0}", _key);
                        state = "Allowed";
                        return true;
                    }

                    state = "NotEligible";
                    return false;
                }
                finally
                {
                    Monitor.Exit(_singleTestLock);
                }
            }
            finally
            {
                _stats.Elapsed(StatsPrefix + " AllowSingleTest", state, stopwatch.Elapsed);
            }
        }

        private bool IsPastWaitDuration()
        {
            return _clock.GetMillisecondTimestamp() > _lastTrippedTimestamp + Properties.TrippedDurationMillis.Value;
        }

        /// <summary>
        /// Checks to see if the breaker should trip, and trips if it should.
        /// </summary>
        /// <returns><code>true</code> if breaker is tripped</returns>
        private bool CheckAndSetTripped()
        {
            var stopwatch = Stopwatch.StartNew();
            var state = "Unknown";
            try
            {
                if (_state == State.Tripped)
                {
                    state = "AlreadyTripped";
                    return true;
                }

                if (!Monitor.TryEnter(_stateChangeLock))
                {
                    state = "MissedLock";
                    return _state == State.Tripped;
                }

                try
                {
                    var snapshot = _metrics.GetSnapshot();

                    // If we haven't met the minimum number of operations needed to trip, don't trip.
                    if (snapshot.Total < Properties.MinimumOperations.Value)
                    {
                        state = "CriteriaNotMet";
                        return false;
                    }

                    // If we're within the error threshold, don't trip.
                    if (snapshot.ErrorPercentage < Properties.ThresholdPercentage.Value)
                    {
                        state = "CriteriaNotMet";
                        return false;
                    }

                    _state = State.Tripped;
                    _lastTrippedTimestamp = _clock.GetMillisecondTimestamp();
                    state = "JustTripped";

                    _stats.Event(StatsPrefix, State.Tripped.ToString(), null);
                    Log.ErrorFormat("Tripped Breaker={0} Operations={1} ErrorPercentage={2} Wait={3}",
                        _key,
                        snapshot.Total,
                        snapshot.ErrorPercentage,
                        Properties.TrippedDurationMillis.Value);

                    return true;
                }
                finally
                {
                    Monitor.Exit(_stateChangeLock);
                }
            }
            finally
            {
                _stats.Elapsed(StatsPrefix + " CheckAndSetTripped", state, stopwatch.Elapsed);
            }
        }

        private enum State
        {
            Fixed,
            Tripped,
        }
    }

    internal class FailurePercentageCircuitBreakerProperties
    {
        private readonly IConfigurableValue<long> _minimumOperations;
        private readonly IConfigurableValue<int> _thresholdPercentage;
        private readonly IConfigurableValue<long> _trippedDurationMillis;
        private readonly IConfigurableValue<bool> _forceTripped;
        private readonly IConfigurableValue<bool> _forceFixed;

        internal FailurePercentageCircuitBreakerProperties(
            IConfigurableValue<long> minimumOperations,
            IConfigurableValue<int> thresholdPercentage,
            IConfigurableValue<long> trippedDurationMillis,
            IConfigurableValue<bool> forceTripped,
            IConfigurableValue<bool> forceFixed)
        {
            _minimumOperations = minimumOperations;
            _thresholdPercentage = thresholdPercentage;
            _trippedDurationMillis = trippedDurationMillis;
            _forceTripped = forceTripped;
            _forceFixed = forceFixed;
        }

        internal IConfigurableValue<long> MinimumOperations { get { return _minimumOperations; } }
        internal IConfigurableValue<int> ThresholdPercentage { get { return _thresholdPercentage; } }
        internal IConfigurableValue<long> TrippedDurationMillis { get { return _trippedDurationMillis; } }
        internal IConfigurableValue<bool> ForceTripped { get { return _forceTripped; } }
        internal IConfigurableValue<bool> ForceFixed { get { return _forceFixed; } }
    }
}