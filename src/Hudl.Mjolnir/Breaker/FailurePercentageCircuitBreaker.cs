using System;
using System.Diagnostics;
using System.Threading;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Clock;

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
        private readonly object _stateChangeLock = new { };
        private readonly object _singleTestLock = new { };

        private readonly IClock _clock;
        private readonly ICommandMetrics _metrics;

        private readonly GroupKey _key;
        private readonly IMetricEvents _metricEvents;
        private readonly IFailurePercentageCircuitBreakerConfig _config;
        private readonly IMjolnirLog<FailurePercentageCircuitBreaker> _log;

        private volatile State _state;
        private long _lastTrippedTimestamp;

        internal FailurePercentageCircuitBreaker(GroupKey key, ICommandMetrics metrics, IMetricEvents metricEvents, IFailurePercentageCircuitBreakerConfig config, IMjolnirLogFactory logFactory)
            : this(key, new UtcSystemClock(), metrics, metricEvents, config, logFactory) { }

        internal FailurePercentageCircuitBreaker(GroupKey key, IClock clock, ICommandMetrics metrics, IMetricEvents metricEvents, IFailurePercentageCircuitBreakerConfig config, IMjolnirLogFactory logFactory)
        {
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _clock = clock ?? throw new ArgumentNullException(nameof(config));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _metricEvents = metricEvents ?? throw new ArgumentNullException(nameof(metricEvents));

            if (logFactory == null)
            {
                throw new ArgumentNullException(nameof(logFactory));
            }

            _key = key;

            _log = logFactory.CreateLog<FailurePercentageCircuitBreaker>();
            if (_log == null)
            {
                throw new InvalidOperationException($"{nameof(IMjolnirLogFactory)} implementation returned null from {nameof(IMjolnirLogFactory.CreateLog)} for type {typeof(FailurePercentageCircuitBreaker)}, please make sure the implementation returns a non-null log for all calls to {nameof(IMjolnirLogFactory.CreateLog)}");
            }

            _state = State.Fixed; // Start off assuming everything's fixed.
            _lastTrippedTimestamp = 0; // 0 is fine since it'll be far less than the first compared value.
        }

        public ICommandMetrics Metrics
        {
            get { return _metrics; }
        }

        public string Name
        {
            get { return _key.Name; }
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
                return;
            }

            _log.Info($"Fixed Breaker={_key}");

            _state = State.Fixed;
            _metrics.Reset();

            _metricEvents.BreakerFixed(Name);
        }

        /// <summary>
        /// </summary>
        /// <returns><code>true</code> if this breaker is allowing operations through</returns>
        public bool IsAllowing()
        {
            var stopwatch = Stopwatch.StartNew();
            var result = true;
            if (_config.GetForceTripped(_key))
            {
                result = false;
            }
            else if (_config.GetForceFixed(_key))
            {
                // If we're forcing, we still want to keep track of the state in case we remove the force.
                CheckAndSetTripped();
                result = true;
            }
            else
            {
                result = !CheckAndSetTripped() || AllowSingleTest();
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <returns><code>true</code> if we should allow a single test operation through the breaker</returns>
        private bool AllowSingleTest()
        {
            var stopwatch = Stopwatch.StartNew();

            if (!Monitor.TryEnter(_singleTestLock))
            {
                return false;
            }

            try
            {
                if (_state == State.Tripped && IsPastWaitDuration())
                {
                    _lastTrippedTimestamp = _clock.GetMillisecondTimestamp();
                    _log.Info($"Allowing single test operation Breaker={_key}");
                    return true;
                }

                return false;
            }
            finally
            {
                Monitor.Exit(_singleTestLock);
            }
        }

        private bool IsPastWaitDuration()
        {
            return _clock.GetMillisecondTimestamp() > _lastTrippedTimestamp + _config.GetTrippedDurationMillis(_key);
        }

        /// <summary>
        /// Checks to see if the breaker should trip, and trips if it should.
        /// </summary>
        /// <returns><code>true</code> if breaker is tripped</returns>
        private bool CheckAndSetTripped()
        {
            var stopwatch = Stopwatch.StartNew();

            if (_state == State.Tripped)
            {
                return true;
            }

            if (!Monitor.TryEnter(_stateChangeLock))
            {
                return _state == State.Tripped;
            }

            try
            {
                var snapshot = _metrics.GetSnapshot();

                // If we haven't met the minimum number of operations needed to trip, don't trip.
                if (snapshot.Total < _config.GetMinimumOperations(_key))
                {
                    return false;
                }

                // If we're within the error threshold, don't trip.
                if (snapshot.ErrorPercentage < _config.GetThresholdPercentage(_key))
                {
                    return false;
                }

                _state = State.Tripped;
                _lastTrippedTimestamp = _clock.GetMillisecondTimestamp();

                _metricEvents.BreakerTripped(Name);
                _log.Error($"Tripped Breaker={_key} Operations={snapshot.Total} ErrorPercentage={snapshot.ErrorPercentage} Wait={_config.GetTrippedDurationMillis(_key)}");

                return true;
            }
            finally
            {
                Monitor.Exit(_stateChangeLock);
            }
        }

        /// <summary>
        /// Whether or not the breaker is tripped. This is a read-out state of the breaker. If
        /// you're attempting to use the breaker, you probably want IsAllowing() instead. This
        /// method is more useful for logging and gauge metrics.
        /// </summary>
        internal bool IsTripped()
        {
            return _state == State.Tripped;
        }

        private enum State
        {
            Fixed,
            Tripped,
        }
    }
}