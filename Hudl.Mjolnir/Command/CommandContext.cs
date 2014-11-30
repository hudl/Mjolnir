using System;
using System.Collections.Concurrent;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Mjolnir.Util;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// Manages all of Mjolnir's pools, breakers, and other state. Also handles
    /// dependency injection for replaceable components (stats, config, etc.).
    /// 
    /// Client code typically doesn't interact with CommandContext other than
    /// to inject dependencies.
    /// </summary>
    public sealed class CommandContext
    {
        private static readonly CommandContext Instance = new CommandContext();

        // Circuit breaker global defaults.
        private static readonly IConfigurableValue<long> DefaultBreakerMinimumOperations = new ConfigurableValue<long>("mjolnir.breaker.default.minimumOperations", 10);
        private static readonly IConfigurableValue<int> DefaultBreakerThresholdPercentage = new ConfigurableValue<int>("mjolnir.breaker.default.thresholdPercentage", 50);
        private static readonly IConfigurableValue<long> DefaultBreakerTrippedDurationMillis = new ConfigurableValue<long>("mjolnir.breaker.default.trippedDurationMillis", 10000);
        private static readonly IConfigurableValue<bool> DefaultBreakerForceTripped = new ConfigurableValue<bool>("mjolnir.breaker.default.forceTripped", false);
        private static readonly IConfigurableValue<bool> DefaultBreakerForceFixed = new ConfigurableValue<bool>("mjolnir.breaker.default.forceFixed", false);
        
        // Circuit breaker metrics global defaults.
        private static readonly IConfigurableValue<long> DefaultMetricsWindowMillis = new ConfigurableValue<long>("mjolnir.metrics.default.windowMillis", 30000);
        private static readonly IConfigurableValue<long> DefaultMetricsSnapshotTtlMillis = new ConfigurableValue<long>("mjolnir.metrics.default.snapshotTtlMillis", 1000);

        // Thread pool global defaults.
        private static readonly IConfigurableValue<int> DefaultPoolThreadCount = new ConfigurableValue<int>("mjolnir.pools.default.threadCount", 10);
        private static readonly IConfigurableValue<int> DefaultPoolQueueLength = new ConfigurableValue<int>("mjolnir.pools.default.queueLength", 10);
        
        // Fallback global defaults.
        private static readonly IConfigurableValue<int> DefaultFallbackMaxConcurrent = new ConfigurableValue<int>("mjolnir.fallback.default.maxConcurrent", 50);
        
        // Instance collections.
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<IIsolationThreadPool>> _pools = new ConcurrentDictionary<GroupKey, Lazy<IIsolationThreadPool>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>> _fallbackSemaphores = new ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>>();

        private IStats _stats = new IgnoringStats();

        private CommandContext() {}

        /// <summary>
        /// Get/set the Stats implementation that all Mjolnir code should use.
        /// 
        /// This should be set as soon as possible if it's going to be implemented.
        /// Other parts of Mjolnir will cache their Stats members, so changing
        /// this after Breakers and Pools have been created won't update the
        /// client for them.
        /// </summary>
        public static IStats Stats
        {
            get { return Instance._stats; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                Instance._stats = value;
            }
        }

        internal static ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return Instance._circuitBreakers.GetOrAddSafe(key, k =>
            {
                var metrics = GetCommandMetrics(key);
                var properties = new FailurePercentageCircuitBreakerProperties(
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".minimumOperations", DefaultBreakerMinimumOperations),
                    new ConfigurableValue<int>("mjolnir.breaker." + key + ".thresholdPercentage", DefaultBreakerThresholdPercentage),
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".trippedDurationMillis", DefaultBreakerTrippedDurationMillis),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceTripped", DefaultBreakerForceTripped),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceFixed", DefaultBreakerForceFixed));

                return new FailurePercentageCircuitBreaker(key, metrics, Stats, properties);
            });
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
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".windowMillis", DefaultMetricsWindowMillis),
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".snapshotTtlMillis", DefaultMetricsSnapshotTtlMillis),
                    Stats));
        }

        internal static IIsolationThreadPool GetThreadPool(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return Instance._pools.GetOrAddSafe(key, k =>
                new StpIsolationThreadPool(
                    key,
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".threadCount", DefaultPoolThreadCount),
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".queueLength", DefaultPoolQueueLength),
                    Stats));
        }

        internal static IIsolationSemaphore GetFallbackSemaphore(GroupKey key)
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
                var maxConcurrent = new ConfigurableValue<int>("mjolnir.fallback." + key + ".maxConcurrent", DefaultFallbackMaxConcurrent);
                return new SemaphoreSlimIsolationSemaphore(key, maxConcurrent, Stats);
            });
        }
    }
}
