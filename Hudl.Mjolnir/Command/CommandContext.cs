using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Mjolnir.Util;
using Hudl.Mjolnir.Bulkhead;
using System.Threading;
using log4net;

namespace Hudl.Mjolnir.Command
{
    internal interface ICommandContext
    {
        IStats Stats { get; set; }
        IMetricEvents MetricEvents { get; set; }
        void IgnoreExceptions(HashSet<Type> types);
        bool IsExceptionIgnored(Type type);
        ICircuitBreaker GetCircuitBreaker(GroupKey key);
        IIsolationThreadPool GetThreadPool(GroupKey key);
        IBulkheadSemaphore GetBulkhead(GroupKey key);
        IIsolationSemaphore GetFallbackSemaphore(GroupKey key);
    }

    /// <summary>
    /// Manages all of Mjolnir's bulkheads, breakers, and other state. Also handles
    /// dependency injection for replaceable components (stats, config, etc.).
    /// 
    /// Client code typically doesn't interact with CommandContext other than
    /// to inject dependencies.
    /// </summary>
    internal class CommandContextImpl : ICommandContext
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CommandContextImpl));

        // Many properties in Mjolnir use a chain of possible configuration values, typically:
        // - Explicitly-configured group value
        // - Explicitly-configured default value
        // - Hard-coded default value
        // 
        // For example, for a breaker named "my-breaker", the application will use the following
        // order for finding a configured value:
        //
        // 1. mjolnir.breaker.my-breaker.thresholdPercentage=90
        // 2. mjolnir.breaker.default.thresholdPercentage=70
        // 3. <default value, hard-coded in CommandContext (50)>
        //
        // See the Mjolnir README for some additional information about configuration.

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
        private readonly ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>> _bulkheads = new ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>> _fallbackSemaphores = new ConcurrentDictionary<GroupKey, Lazy<IIsolationSemaphore>>();

        // This is a Dictionary only because there's no great concurrent Set type available. Just
        // use the keys if checking for a type.
        private readonly ConcurrentDictionary<Type, bool> _ignoredExceptionTypes = new ConcurrentDictionary<Type, bool>();

        private IStats _stats = new IgnoringStats();
        public IStats Stats
        {
            get { return _stats; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                _stats = value;
            }
        }

        private IMetricEvents _metricEvents = new IgnoringMetricEvents();
        public IMetricEvents MetricEvents
        {
            get { return _metricEvents; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentException();
                }
                _metricEvents = value;
            }
        }

        public void IgnoreExceptions(HashSet<Type> types)
        {
            if (types == null || types.Count == 0)
            {
                return;
            }

            foreach (var type in types)
            {
                _ignoredExceptionTypes.TryAdd(type, true);
            }
        }

        public bool IsExceptionIgnored(Type type)
        {
            return _ignoredExceptionTypes.ContainsKey(type);
        }

        public ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return _circuitBreakers.GetOrAddSafe(key, k =>
            {
                var metrics = GetCommandMetrics(key);
                var properties = new FailurePercentageCircuitBreakerProperties(
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".minimumOperations", DefaultBreakerMinimumOperations),
                    new ConfigurableValue<int>("mjolnir.breaker." + key + ".thresholdPercentage", DefaultBreakerThresholdPercentage),
                    new ConfigurableValue<long>("mjolnir.breaker." + key + ".trippedDurationMillis", DefaultBreakerTrippedDurationMillis),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceTripped", DefaultBreakerForceTripped),
                    new ConfigurableValue<bool>("mjolnir.breaker." + key + ".forceFixed", DefaultBreakerForceFixed));

                return new FailurePercentageCircuitBreaker(key, metrics, Stats, MetricEvents, properties);
            });
        }

        private ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return _metrics.GetOrAddSafe(key, k =>
                new StandardCommandMetrics(
                    key,
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".windowMillis", DefaultMetricsWindowMillis),
                    new ConfigurableValue<long>("mjolnir.metrics." + key + ".snapshotTtlMillis", DefaultMetricsSnapshotTtlMillis),
                    Stats));
        }

        public IIsolationThreadPool GetThreadPool(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return _pools.GetOrAddSafe(key, k =>
                new StpIsolationThreadPool(
                    key,
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".threadCount", DefaultPoolThreadCount),
                    new ConfigurableValue<int>("mjolnir.pools." + key + ".queueLength", DefaultPoolQueueLength),
                    Stats,
                    MetricEvents));
        }

        /// <summary>
        /// Callers should keep a local reference to the bulkhead object they receive from this
        /// method, ensuring that they call TryEnter and Release on the same object reference.
        /// Phrased differently: don't re-retrieve the bulkhead before calling Release().
        /// </summary>
        public IBulkheadSemaphore GetBulkhead(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            var holder = _bulkheads.GetOrAddSafe(key,
                k => new SemaphoreBulkheadHolder(key, _metricEvents),
                LazyThreadSafetyMode.ExecutionAndPublication);

            return holder.Bulkhead;
        }

        public IIsolationSemaphore GetFallbackSemaphore(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return _fallbackSemaphores.GetOrAddSafe(key, k =>
            {
                // For now, the default here is 5x the default pool threadCount, with the presumption that
                // several commands may using the same pool, and we should therefore try to allow for a bit
                // more concurrent fallback execution.
                var maxConcurrent = new ConfigurableValue<int>("mjolnir.fallback." + key + ".maxConcurrent", DefaultFallbackMaxConcurrent);
                return new SemaphoreSlimIsolationSemaphore(key, maxConcurrent, Stats);
            });
        }
        
        // In order to dynamically change semaphore limits, we replace the semaphore on config
        // change events. We should never destroy the holder once it's been created - we may
        // replace its internal members, but the holder should remain for the lifetime of the
        // app to ensure consistent concurrency.
        private class SemaphoreBulkheadHolder
        {
            // Note: changing the default value at runtime won't trigger a rebuild of the
            // semaphores; that will require an app restart.
            private static readonly IConfigurableValue<int> DefaultBulkheadMaxConcurrent = new ConfigurableValue<int>("mjolnir.bulkheads.default.maxConcurrent", 10);
            private static readonly IConfigurableValue<long> ConfigGaugeIntervalMillis = new ConfigurableValue<long>("mjolnir.bulkheadConfigGaugeIntervalMillis", 60000);

            // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
            // Keep the reference around, we have a change handler attached.
            private readonly IConfigurableValue<int> _config;
            private readonly GaugeTimer _timer;
            // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

            private IBulkheadSemaphore _bulkhead;
            public IBulkheadSemaphore Bulkhead { get { return _bulkhead; } }

            private readonly IMetricEvents _metricEvents;

            public SemaphoreBulkheadHolder(GroupKey key, IMetricEvents metricEvents)
            {
                // The order of things here is very intentional.
                // We create the configurable value first, retrieve its current value, and then
                // initialize the semaphore bulkhead. We register the change handler after that.
                // That ought to help avoid a situation where we might fire a config change handler
                // before we add the semaphore to the dictionary, potentially trying to add two
                // entries with different values in rapid succession.

                var configKey = "mjolnir.bulkheads." + key + ".maxConcurrent";
                _config = new ConfigurableValue<int>(configKey, DefaultBulkheadMaxConcurrent);

                var value = _config.Value;
                _bulkhead = new SemaphoreBulkhead(key, value);

                // On change, we'll replace the bulkhead. The assumption here is that a caller
                // using the bulkhead will have kept a local reference to the bulkhead that they
                // acquired a lock on, and will release the lock on that bulkhead and not one that
                // has been replaced after a config change.
                _config.AddChangeHandler(newLimit =>
                {
                    if (newLimit < 0)
                    {
                        Log.ErrorFormat("Semaphore bulkhead config {0} changed to an invalid limit of {0}, the bulkhead will not be changed",
                            configKey,
                            newLimit);
                        return;
                    }
                    
                    _bulkhead = new SemaphoreBulkhead(key, newLimit);
                });

                _timer = new GaugeTimer((source, args) =>
                {
                    _metricEvents.BulkheadConfigGauge(_bulkhead.Name, "semaphore", _config.Value);
                }, ConfigGaugeIntervalMillis);
            }
        }
    }
    
    /// <summary>
    /// Handles some dependency injection and configuration for Mjolnir.
    /// </summary>
    public static class CommandContext
    {
        internal static readonly ICommandContext Current = new CommandContextImpl();

        /// <summary>
        /// Get/set the Stats implementation that all Mjolnir code should use.
        /// 
        /// This should be set as soon as possible if it's going to be implemented.
        /// Other parts of Mjolnir will cache their Stats members, so changing
        /// this after Breakers and Pools have been created won't update the
        /// client for them.
        /// </summary>
        [Obsolete("Use MetricEvents instead")]
        public static IStats Stats
        {
            get { return Current.Stats; }
            set { Current.Stats = value; }
        }

        /// <summary>
        /// Get/set the MetricEvents implementation that all Mjolnir code should use.
        /// 
        /// This should be set as soon as possible if it's going to be implemented.
        /// Other parts of Mjolnir will cache their Stats members, so changing
        /// this after Breakers and Pools have been created won't update the
        /// client for them.
        /// </summary>
        public static IMetricEvents MetricEvents
        {
            get { return Current.MetricEvents; }
            set { Current.MetricEvents = value; }
        }

        /// <summary>
        /// Ignored exception types won't count toward breakers tripping or other error counters.
        /// Useful for things like validation, where the system isn't having any problems and the
        /// caller needs to validate before invoking. This list is most applicable when using
        /// [Command] attributes, since extending Command offers the ability to catch these types
        /// specifically within Execute() - though there may still be some benefit in extended
        /// Commands for validation-like situations where throwing is still desired.
        /// </summary>
        public static void IgnoreExceptions(HashSet<Type> types)
        {
            Current.IgnoreExceptions(types);
        }
    }
}
