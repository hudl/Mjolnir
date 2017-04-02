using System;
using Amib.Threading;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;

namespace Hudl.Mjolnir.ThreadPool
{
    /// <summary>
    /// IIsolationThreadPool that uses a backing SmartThreadPool.
    /// </summary>
    internal class StpIsolationThreadPool : IIsolationThreadPool
    {
        private static readonly IConfigurableValue<long> ConfigGaugeIntervalMillis = new ConfigurableValue<long>("mjolnir.bulkheadConfigGaugeIntervalMillis", 60000);

        private readonly GroupKey _key;
        private readonly SmartThreadPool _pool;
        private readonly IMetricEvents _metricEvents;

        private readonly IConfigurableValue<int> _threadCount;
        private readonly IConfigurableValue<int> _queueLength;
        
        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _metricsTimer;
        // ReSharper restore NotAccessedField.Local

        internal StpIsolationThreadPool(GroupKey key, IConfigurableValue<int> threadCount, IConfigurableValue<int> queueLength, IMetricEvents metricEvents, IConfigurableValue<long> gaugeIntervalMillisOverride = null)
        {
            _key = key;
            _threadCount = threadCount;
            _queueLength = queueLength;
            
            if (metricEvents == null)
            {
                throw new ArgumentNullException("metricEvents");
            }
            _metricEvents = metricEvents;

            var count = _threadCount.Value;
            var info = new STPStartInfo
            {
                ThreadPoolName = _key.Name,
                MinWorkerThreads = count,
                MaxWorkerThreads = count,
                MaxQueueLength = queueLength.Value,
                AreThreadsBackground = true,
                UseCallerExecutionContext = true,
                UseCallerHttpContext = true
            };

            _pool = new SmartThreadPool(info);
            
            _metricsTimer = new GaugeTimer((source, args) =>
            {
                _metricEvents.BulkheadConfigGauge(Name, "pool", queueLength.Value + threadCount.Value);
            }, ConfigGaugeIntervalMillis);

            _threadCount.AddChangeHandler(UpdateThreadCount);
            _queueLength.AddChangeHandler(UpdateQueueLength);
        }
        
        public string Name { get { return _key.Name; } }

        public void Start()
        {
            _pool.Start();
        }

        public IWorkItem<TResult> Enqueue<TResult>(System.Func<TResult> func)
        {
            try
            {
                var workItem = _pool.QueueWorkItem(new Amib.Threading.Func<TResult>(func));
                return new StpWorkItem<TResult>(workItem);
            }
            catch (QueueRejectedException)
            {
                throw new IsolationThreadPoolRejectedException();
            }
        }

        private void UpdateThreadCount(int threadCount)
        {
            _pool.MaxThreads = threadCount;
        }

        private void UpdateQueueLength(int queueLength)
        {
            _pool.MaxQueueLength = queueLength;
        }
    }
}