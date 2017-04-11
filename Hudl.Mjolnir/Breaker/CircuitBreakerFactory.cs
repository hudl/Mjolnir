using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
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

        private readonly ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();

        public CircuitBreakerFactory(IMetricEvents metricEvents, IFailurePercentageCircuitBreakerConfig breakerConfig, IMjolnirLogFactory logFactory)
        {
            // No null checks on parameters; we don't use them, we're just passing them through to
            // the objects we're creating.

            _metricEvents = metricEvents;
            _breakerConfig = breakerConfig;
            _logFactory = logFactory;
        }

        public ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            return _circuitBreakers.GetOrAdd(key, new Lazy<ICircuitBreaker>(() => CircuitBreakerValueFactory(key), LazyThreadSafetyMode.PublicationOnly)).Value;
        }
        
        private ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            return _metrics.GetOrAdd(key, new Lazy<ICommandMetrics>(() => new StandardCommandMetrics(key, _breakerConfig, _logFactory), LazyThreadSafetyMode.PublicationOnly)).Value;
        }

        private ICircuitBreaker CircuitBreakerValueFactory(GroupKey key)
        {
            var metrics = GetCommandMetrics(key);
            return new FailurePercentageCircuitBreaker(key, metrics, _metricEvents, _breakerConfig, _logFactory);
        }
    }
}
