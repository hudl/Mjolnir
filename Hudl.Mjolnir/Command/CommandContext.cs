using System;
using System.Collections.Concurrent;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Mjolnir.Util;
using Hudl.Riemann;

namespace Hudl.Mjolnir.Command
{
    internal sealed class CommandContext
    {
        private static readonly CommandContext Instance = new CommandContext();

        private readonly ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<IIsolationThreadPool>> _pools = new ConcurrentDictionary<GroupKey, Lazy<IIsolationThreadPool>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>> _fallbackSemaphores = new ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>>();

        private IRiemann _riemann = RiemannStats.Instance;

        /// <summary>
        /// Get/set the default Riemann client that all Mjolnir code should use.
        /// Defaults to RiemannStats.Instance, which is fine for most
        /// situations. Useful for controlling Riemann for system testing.
        /// 
        /// This should be set as soon as possible if you're going to change it.
        /// Other parts of Mjolnir will cache their Riemann clients, so changing
        /// this after Breakers and Pools have been created won't update the
        /// client for them.
        /// 
        /// <remarks>If we ever build a DI framework into Mjolnir, we should
        /// switch this over to it.</remarks>
        /// </summary>
        internal IRiemann DefaultRiemannClient
        {
            get { return _riemann; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                _riemann = value;
            }
        }

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        private CommandContext()
        {
            _timer = new GaugeTimer((source, args) =>
            {
                _riemann.Gauge("mjolnir context breakers", null, _circuitBreakers.Count);
                _riemann.Gauge("mjolnir context metrics", null, _metrics.Count);
                _riemann.Gauge("mjolnir context pools", null, _pools.Count);
                _riemann.Gauge("mjolnir context semaphores", null, _fallbackSemaphores.Count);
            });
        }

        internal static IRiemann Riemann
        {
            get { return Instance.DefaultRiemannClient; }
            set { Instance.DefaultRiemannClient = value; }
        }

        public static ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            var metrics = GetCommandMetrics(key);

            var properties = new FailurePercentageCircuitBreakerProperties(
                new ConfigurableValue<long>("mjolnir.breaker." + key + ".minimumOperations", 10),
                new ConfigurableValue<int>("mjolnir.breaker." + key + ".thresholdPercentage", 50),
                new ConfigurableValue<long>("mjolnir.breaker." + key + ".trippedDurationMillis", 10000),
                new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceTripped", false),
                new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceFixed", false));

            return Instance._circuitBreakers.GetOrAddSafe(key, k =>
                new FailurePercentageCircuitBreaker(key, metrics, properties));
        }

        private static ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return Instance._metrics.GetOrAddSafe(key, k =>
                new StandardCommandMetrics(
                    key,
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".windowMillis", 30000),
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".snapshotTtlMillis", 1000)));
        }

        public static IIsolationThreadPool GetThreadPool(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return Instance._pools.GetOrAddSafe(key, k =>
                new StpIsolationThreadPool(
                    key,
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".threadCount", 10),
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".queueLength", 10)));
        }

        public static IIsolationSemaphore GetFallbackSemaphore(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return Instance._fallbackSemaphores.GetOrAddSafe(key, k =>
            {
                // For now, the default here is 5x the default pool threadCount, with the presumption that
                // several commands may using the same pool, and we should therefore try to allow for a bit
                // more concurrent fallback execution.
                var maxConcurrent = new ConfigurableValue<int>("mjolnir.fallback." + key + ".maxConcurrent", 50);
                return new SemaphoreSlimIsolationSemaphore(key, maxConcurrent);
            });
        }
    }
}
