using System.Threading;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;
using Hudl.Riemann;

namespace Hudl.Mjolnir.ThreadPool
{
    internal class SemaphoreSlimIsolationSemaphore : IIsolationSemaphore
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly GroupKey _key;
        private readonly int _maxConcurrent;
        private readonly IRiemann _riemann;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        public SemaphoreSlimIsolationSemaphore(GroupKey key, IConfigurableValue<int> maxConcurrent)
            : this(key, maxConcurrent, null) {}

        public SemaphoreSlimIsolationSemaphore(GroupKey key, IConfigurableValue<int> maxConcurrent, IRiemann riemann)
        {
            _key = key;
            _riemann = (riemann ?? RiemannStats.Instance);

            // Note: Changing the semaphore maximum at runtime is not currently supported.
            _maxConcurrent = maxConcurrent.Value;
            _semaphore = new SemaphoreSlim(_maxConcurrent);

            _timer = new GaugeTimer((source, args) =>
            {
                var count = _semaphore.CurrentCount;
                _riemann.ConfigGauge(RiemannPrefix + " conf.maxConcurrent", _maxConcurrent);
                _riemann.Gauge(RiemannPrefix + " available", (count == 0 ? "Full" : "Available"), count);
            });
        }

        private string RiemannPrefix
        {
            get { return "mjolnir fallback-semaphore " + _key; }
        }

        public bool TryEnter()
        {
            return _semaphore.Wait(0);
        }

        public void Release()
        {
            _semaphore.Release();
        }
    }
}