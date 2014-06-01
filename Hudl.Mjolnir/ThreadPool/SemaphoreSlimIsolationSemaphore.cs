using System;
using System.Threading;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;

namespace Hudl.Mjolnir.ThreadPool
{
    internal class SemaphoreSlimIsolationSemaphore : IIsolationSemaphore
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly GroupKey _key;
        private readonly int _maxConcurrent;
        private readonly IStats _stats;

        // ReSharper disable NotAccessedField.Local
        // Don't let these get garbage collected.
        private readonly GaugeTimer _timer;
        // ReSharper restore NotAccessedField.Local

        internal SemaphoreSlimIsolationSemaphore(GroupKey key, IConfigurableValue<int> maxConcurrent, IStats stats, IConfigurableValue<long> gaugeIntervalMillisOverride = null)
        {
            _key = key;

            if (stats == null)
            {
                throw new ArgumentNullException("stats");
            }

            _stats = stats;

            // Note: Changing the semaphore maximum at runtime is not currently supported.
            _maxConcurrent = maxConcurrent.Value;
            _semaphore = new SemaphoreSlim(_maxConcurrent);

            _timer = new GaugeTimer((source, args) =>
            {
                var count = _semaphore.CurrentCount;
                _stats.ConfigGauge(StatsPrefix + " conf.maxConcurrent", _maxConcurrent);
                _stats.Gauge(StatsPrefix + " available", (count == 0 ? "Full" : "Available"), count);
            }, gaugeIntervalMillisOverride);
        }

        private string StatsPrefix
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