using System;
using System.Diagnostics;
using Amib.Threading;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;
using Hudl.Riemann;

namespace Hudl.Mjolnir.ThreadPool
{
    /// <summary>
    /// IIsolationThreadPool that uses a backing SmartThreadPool.
    /// </summary>
    internal class StpIsolationThreadPool : IIsolationThreadPool
    {
        private readonly GroupKey _key;
        private readonly SmartThreadPool _pool;
        private readonly IRiemann _riemann;

        private readonly IConfigurableValue<int> _threadCount;
        private readonly IConfigurableValue<int> _queueLength;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        internal StpIsolationThreadPool(GroupKey key, IConfigurableValue<int> threadCount, IConfigurableValue<int> queueLength, IRiemann riemann, IConfigurableValue<long> gaugeIntervalMillisOverride = null)
        {
            _key = key;
            _threadCount = threadCount;
            _queueLength = queueLength;

            if (riemann == null)
            {
                throw new ArgumentNullException("riemann");
            }
            _riemann = riemann;

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

            _timer = new GaugeTimer((source, args) =>
            {
                _riemann.Gauge(RiemannPrefix + " activeThreads", null, _pool.ActiveThreads);
                _riemann.Gauge(RiemannPrefix + " inUseThreads", null, _pool.InUseThreads);

                // Note: Don't use _pool.WaitingCallbacks. It has the potential to get locked out by
                // queue/dequeue operations, and may block here if the pool's getting queued into heavily.
                _riemann.Gauge(RiemannPrefix + " pendingCompletion", null, _pool.CurrentWorkItemsCount);

                _riemann.ConfigGauge(RiemannPrefix + " conf.threadCount", _threadCount.Value);
                _riemann.ConfigGauge(RiemannPrefix + " conf.queueLength", _queueLength.Value);
            }, gaugeIntervalMillisOverride);

            _pool.OnThreadInitialization += () => _riemann.Event(RiemannPrefix + " thread", "Initialized", null);
            _pool.OnThreadTermination += () => _riemann.Event(RiemannPrefix + " thread", "Terminated", null);

            _threadCount.AddChangeHandler(UpdateThreadCount);
            _queueLength.AddChangeHandler(UpdateQueueLength);
        }

        private string RiemannPrefix
        {
            get { return "mjolnir pool " + _key; }
        }

        public void Start()
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                _pool.Start();
            }
            finally
            {
                _riemann.Elapsed(RiemannPrefix + " Start", null, stopwatch.Elapsed);
            }
        }

        public IWorkItem<TResult> Enqueue<TResult>(System.Func<TResult> func)
        {
            var stopwatch = Stopwatch.StartNew();
            var state = "Enqueued";
            try
            {
                var workItem = _pool.QueueWorkItem(new Amib.Threading.Func<TResult>(func));
                return new StpWorkItem<TResult>(workItem);
            }
            catch (QueueRejectedException)
            {
                state = "Rejected";
                throw new IsolationThreadPoolRejectedException();
            }
            finally
            {
                _riemann.Elapsed(RiemannPrefix + " Enqueue", state, stopwatch.Elapsed);
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