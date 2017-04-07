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
        private readonly IStandardCommandMetricsConfig _metricsConfig;
        private readonly IMjolnirLogFactory _logFactory;

        private readonly ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();

        public CircuitBreakerFactory(IMetricEvents metricEvents, IFailurePercentageCircuitBreakerConfig breakerConfig, IStandardCommandMetricsConfig metricsConfig, IMjolnirLogFactory logFactory)
        {
            // No null checks on parameters; we don't use them, we're just passing them through to
            // the objects we're creating.

            _metricEvents = metricEvents;
            _breakerConfig = breakerConfig;
            _metricsConfig = metricsConfig;
            _logFactory = logFactory;
        }

        public ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            return _circuitBreakers.GetOrAddSafe(key, k =>
            {
                var metrics = GetCommandMetrics(key);
                return new FailurePercentageCircuitBreaker(key, metrics, _metricEvents, _breakerConfig, _logFactory);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            return _metrics.GetOrAddSafe(key, k =>
                new StandardCommandMetrics(key, _metricsConfig, _logFactory),
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
