using System;
using System.Collections.Concurrent;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Isolation;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
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
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".minimumOperations", 10),
                    new ConfigurableValue<int>("mjolnir.breaker." + key + ".thresholdPercentage", 50),
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".trippedDurationMillis", 10000),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceTripped", false),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceFixed", false));

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
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".windowMillis", 30000),
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".snapshotTtlMillis", 1000),
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
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".threadCount", 10),
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".queueLength", 10),
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
                var maxConcurrent = new ConfigurableValue<int>("mjolnir.fallback." + key + ".maxConcurrent", 50);
                return new SemaphoreSlimIsolationSemaphore(key, maxConcurrent, Stats);
            });
        }
    }
}
