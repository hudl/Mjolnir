using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;
using System;
using System.Collections.Concurrent;
using System.Threading;


namespace Hudl.Mjolnir.Bulkhead
{
    internal class BulkheadFactory : IBulkheadFactory
    {
        private readonly ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>> _bulkheads = new ConcurrentDictionary<GroupKey, Lazy<SemaphoreBulkheadHolder>>();

        private readonly IMetricEvents _metricEvents;
        private readonly IBulkheadConfig _bulkheadConfig;
        private readonly IMjolnirLogFactory _logFactory;

        public BulkheadFactory(IMetricEvents metricEvents, IBulkheadConfig bulkheadConfig, IMjolnirLogFactory logFactory)
        {
            // No null checks on parameters; we don't use them, we're just passing them through to
            // the objects we're creating.

            _metricEvents = metricEvents;
            _bulkheadConfig = bulkheadConfig;
            _logFactory = logFactory;
        }

        /// <summary>
        /// Callers should keep a local reference to the bulkhead object they receive from this
        /// method, ensuring that they call TryEnter and Release on the same object reference.
        /// Phrased differently: don't re-retrieve the bulkhead before calling Release().
        /// </summary>
        public ISemaphoreBulkhead GetBulkhead(GroupKey key)
        {
            return GetBulkheadHolder(key).Bulkhead;
        }
        
        // This only exists so that unit tests can get access to the holder to trigget its config
        // change handler.
        internal SemaphoreBulkheadHolder GetBulkheadHolder(GroupKey key)
        {
            // Use LazyThreadSafetyMode.PublicationOnly. PublicationOnly means that multiple
            // threads that attempt to initialize the lazy value at the same time might each create
            // a SemaphoreBulkheadHolder, but the first to complete will get set as the Lazy's
            // Value. Creating multiple is fine because the SemaphoreBulkheadHolders are
            // lightweight.
            //
            // PublicationOnly is important because the SemaphoreBulkheadHolder may throw an
            // exception, particularly in the case where the MaxConcurrent config value is
            // initially an invalid value. Other LazyThreadSafetyModes will cache that exception
            // and re-throw it on all requests for Lazy.Value; PublicationOnly will not cache
            // the value, which means if the config value later changes to a valid value, the
            // initialization here will work and start returning a valid Bulkhead instead of an
            // exception.
            return _bulkheads.GetOrAdd(key, new Lazy<SemaphoreBulkheadHolder>(() => new SemaphoreBulkheadHolder(key, _metricEvents, _bulkheadConfig, _logFactory), LazyThreadSafetyMode.PublicationOnly)).Value;
        }

        // In order to dynamically change semaphore limits, we replace the semaphore on config
        // change events. We should never destroy the holder once it's been created - we may
        // replace its internal members, but the holder should remain for the lifetime of the
        // app to ensure consistent concurrency.
        internal class SemaphoreBulkheadHolder
        {
            // ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
            private readonly GaugeTimer _timer;
            // ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

            private readonly GroupKey _key;

            private ISemaphoreBulkhead _bulkhead;
            public ISemaphoreBulkhead Bulkhead { get { return _bulkhead; } }

            private readonly IMetricEvents _metricEvents;
            private readonly IBulkheadConfig _config;
            private readonly IMjolnirLog _log;

            public SemaphoreBulkheadHolder(GroupKey key, IMetricEvents metricEvents, IBulkheadConfig config, IMjolnirLogFactory logFactory)
            {
                _key = key;
                _metricEvents = metricEvents ?? throw new ArgumentNullException(nameof(metricEvents));
                _config = config ?? throw new ArgumentNullException(nameof(config));

                if (logFactory == null)
                {
                    throw new ArgumentNullException(nameof(logFactory));
                }

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
                _bulkhead = new SemaphoreBulkhead(_key, value);

                // On change, we'll replace the bulkhead. The assumption here is that a caller
                // using the bulkhead will have kept a local reference to the bulkhead that they
                // acquired a lock on, and will release the lock on that bulkhead and not one that
                // has been replaced after a config change.
                _config.AddChangeHandler<int>(_key, UpdateMaxConcurrent);

                _timer = new GaugeTimer(state =>
                {
                    try
                    {
                        _metricEvents.BulkheadConfigGauge(_bulkhead.Name, "semaphore", _config.GetMaxConcurrent(key));
                    }
                    catch (Exception e)
                    {
                        _log.Error($"Error sending {nameof(IMetricEvents.BulkheadConfigGauge)} metric event", e);
                    }
                });
            }
            
            internal void UpdateMaxConcurrent(int newLimit)
            {
                if (!IsValidMaxConcurrent(newLimit))
                {
                    _log.Error($"Semaphore bulkhead config {_config.GetConfigKey(_key)} changed to an invalid limit of {newLimit}, the bulkhead will not be changed");
                    return;
                }

                _bulkhead = new SemaphoreBulkhead(_key, newLimit);
            }

            private bool IsValidMaxConcurrent(int limit)
            {
                return limit >= 0;
            }
        }
    }
}
