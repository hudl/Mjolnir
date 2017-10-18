using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Util;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Hudl.Mjolnir.Breaker
{
    internal class CircuitBreakerFactory : ICircuitBreakerFactory
    {
        private readonly IMetricEvents _metricEvents;
        private readonly IFailurePercentageCircuitBreakerConfig _breakerConfig;
        private readonly IMjolnirLogFactory _logFactory;
        private readonly IMjolnirLog _log;

        // ReSharper disable NotAccessedField.Local
        // Don't let this get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        private readonly ConcurrentDictionary<GroupKey, Lazy<FailurePercentageCircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<FailurePercentageCircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();

        public CircuitBreakerFactory(IMetricEvents metricEvents, IFailurePercentageCircuitBreakerConfig breakerConfig, IMjolnirLogFactory logFactory)
        {
            _metricEvents = metricEvents ?? throw new ArgumentNullException(nameof(metricEvents));
            _breakerConfig = breakerConfig ?? throw new ArgumentNullException(nameof(breakerConfig));
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));

            _log = logFactory.CreateLog(typeof(FailurePercentageCircuitBreaker));
            if (_log == null)
            {
                throw new InvalidOperationException($"{nameof(IMjolnirLogFactory)} implementation returned null from {nameof(IMjolnirLogFactory.CreateLog)} for type {typeof(CircuitBreakerFactory)}, please make sure the implementation returns a non-null log for all calls to {nameof(IMjolnirLogFactory.CreateLog)}");
            }

            _timer = new GaugeTimer(state =>
            {
                try
                {
                    var keys = _circuitBreakers.Keys;
                    foreach (var key in keys)
                    {
                        if (_circuitBreakers.TryGetValue(key, out Lazy<FailurePercentageCircuitBreaker> lazy) && lazy.IsValueCreated)
                        {
                            var breaker = lazy.Value;
                            _metricEvents.BreakerGauge(
                                breaker.Name,
                                _breakerConfig.GetMinimumOperations(key),
                                _breakerConfig.GetWindowMillis(key),
                                _breakerConfig.GetThresholdPercentage(key),
                                _breakerConfig.GetTrippedDurationMillis(key),
                                _breakerConfig.GetForceTripped(key),
                                _breakerConfig.GetForceFixed(key),
                                breaker.IsTripped(),
                                breaker.Metrics.SuccessCount,
                                breaker.Metrics.FailureCount);
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.Error($"Error sending {nameof(IMetricEvents.BreakerGauge)} metric event", e);
                }
            });
        }

        public ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            return _circuitBreakers.GetOrAdd(key, new Lazy<FailurePercentageCircuitBreaker>(() => CircuitBreakerValueFactory(key), LazyThreadSafetyMode.PublicationOnly)).Value;
        }
        
        private ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            return _metrics.GetOrAdd(key, new Lazy<ICommandMetrics>(() => new StandardCommandMetrics(key, _breakerConfig, _logFactory), LazyThreadSafetyMode.PublicationOnly)).Value;
        }

        private FailurePercentageCircuitBreaker CircuitBreakerValueFactory(GroupKey key)
        {
            var metrics = GetCommandMetrics(key);
            return new FailurePercentageCircuitBreaker(key, metrics, _metricEvents, _breakerConfig, _logFactory);
        }
    }
}
