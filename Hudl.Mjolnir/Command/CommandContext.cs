using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Util;
using Hudl.Mjolnir.Bulkhead;
using System.Threading;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.Events;
using Hudl.Mjolnir.Log;

namespace Hudl.Mjolnir.Command
{
    internal interface ICommandContext
    {
        IMetricEvents MetricEvents { get; set; }
        ICircuitBreaker GetCircuitBreaker(GroupKey key);
        IBulkheadSemaphore GetBulkhead(GroupKey key);
    }

    /// <summary>
    /// Manages all of Mjolnir's bulkheads, breakers, and other state. Also handles
    /// dependency injection for replaceable components (metrics, config, etc.).
    /// 
    /// Client code typically doesn't interact with CommandContext other than
    /// to inject dependencies.
    /// </summary>
    internal class CommandContextImpl : ICommandContext
    {
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
        
        // Instance collections.
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>> _circuitBreakers = new ConcurrentDictionary<GroupKey, Lazy<ICircuitBreaker>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>> _metrics = new ConcurrentDictionary<GroupKey, Lazy<ICommandMetrics>>();
        private readonly ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>> _bulkheads = new ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>>();
        
        // TODO make injectable
        private readonly IMjolnirConfig _config;
        private readonly IFailurePercentageCircuitBreakerConfig _breakerConfig;
        private readonly IStandardCommandMetricsConfig _metricsConfig;
        private readonly IBulkheadConfig _bulkheadConfig;
        private readonly IMjolnirLogFactory _logFactory;

        public CommandContextImpl()
        {
            _config = new DefaultValueConfig();
            _breakerConfig = new FailurePercentageCircuitBreakerConfig(_config);
            _metricsConfig = new StandardCommandMetricsConfig(_config);
            _bulkheadConfig = new BulkheadConfig(_config);
            _logFactory = new DefaultMjolnirLogFactory();
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
        
        public ICircuitBreaker GetCircuitBreaker(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _circuitBreakers.GetOrAddSafe(key, k =>
            {
                var metrics = GetCommandMetrics(key);
                return new FailurePercentageCircuitBreaker(key, metrics, MetricEvents, _breakerConfig, _logFactory);
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        private ICommandMetrics GetCommandMetrics(GroupKey key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _metrics.GetOrAddSafe(key, k =>
                new StandardCommandMetrics(key, _metricsConfig, _logFactory),
                LazyThreadSafetyMode.ExecutionAndPublication);
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
                throw new ArgumentNullException(nameof(key));
            }

            var holder = _bulkheads.GetOrAddSafe(key,
                k => new SemaphoreBulkheadHolder(key, _metricEvents, _bulkheadConfig, _logFactory),
                LazyThreadSafetyMode.ExecutionAndPublication);

            return holder.Bulkhead;
        }
        
        // In order to dynamically change semaphore limits, we replace the semaphore on config
        // change events. We should never destroy the holder once it's been created - we may
        // replace its internal members, but the holder should remain for the lifetime of the
        // app to ensure consistent concurrency.
        private class SemaphoreBulkheadHolder
        {
            // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
            private readonly GaugeTimer _timer;
            // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable
            
            private IBulkheadSemaphore _bulkhead;
            public IBulkheadSemaphore Bulkhead { get { return _bulkhead; } }

            private readonly IMetricEvents _metricEvents;
            private readonly IBulkheadConfig _config;
            private readonly IMjolnirLog _log;

            public SemaphoreBulkheadHolder(GroupKey key, IMetricEvents metricEvents, IBulkheadConfig config, IMjolnirLogFactory logFactory)
            {
                if (metricEvents == null)
                {
                    throw new ArgumentNullException(nameof(metricEvents));
                }

                if (config == null)
                {
                    throw new ArgumentNullException(nameof(config));
                }

                if (logFactory == null)
                {
                    throw new ArgumentNullException(nameof(logFactory));
                }

                _metricEvents = metricEvents;
                _config = config;

                _log = logFactory.CreateLog(typeof(SemaphoreBulkheadHolder));

                if (_log == null)
                {
                    throw new InvalidOperationException($"{nameof(IMjolnirLogFactory)} implementation returned null from {nameof(IMjolnirLogFactory.CreateLog)} for type {typeof(SemaphoreBulkheadHolder)}, please make sure the implementation returns a non-null log for all calls to {nameof(IMjolnirLogFactory.CreateLog)}");
                }

                // The order of things here is very intentional.
                // We get the MaxConcurrent value first and then initialize the semaphore bulkhead.
                // The change handler is registered after that. The order ought to help avoid a
                // situation where we might fire a config change handler before we add the
                // semaphore to the dictionary, potentially trying to add two entries with
                // different values in rapid succession.

                var value = _config.GetMaxConcurrent(key);
                
                _bulkhead = new SemaphoreBulkhead(key, value);

                // On change, we'll replace the bulkhead. The assumption here is that a caller
                // using the bulkhead will have kept a local reference to the bulkhead that they
                // acquired a lock on, and will release the lock on that bulkhead and not one that
                // has been replaced after a config change.
                _config.AddChangeHandler<int>(key, newLimit =>
                {
                    if (newLimit < 0)
                    {
                        _log.Error($"Semaphore bulkhead config {_config.GetConfigKey(key)} changed to an invalid limit of {newLimit}, the bulkhead will not be changed");
                        return;
                    }
                    
                    _bulkhead = new SemaphoreBulkhead(key, newLimit);
                });

                _timer = new GaugeTimer((source, args) =>
                {
                    _metricEvents.BulkheadConfigGauge(_bulkhead.Name, "semaphore", _config.GetMaxConcurrent(key));
                });
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
        /// Get/set the MetricEvents implementation that all Mjolnir code should use.
        /// 
        /// This should be set as soon as possible if it's going to be implemented.
        /// Other parts of Mjolnir will cache their MetricEvents members, so changing
        /// this after Breakers and Bulkheads have been created won't update the
        /// client for them.
        /// </summary>
        public static IMetricEvents MetricEvents
        {
            get { return Current.MetricEvents; }
            set { Current.MetricEvents = value; }
        }
    }
}
