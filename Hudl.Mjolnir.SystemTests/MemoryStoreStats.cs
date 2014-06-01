using System;
using System.Collections.Generic;
using Hudl.Mjolnir.External;

namespace Hudl.Mjolnir.SystemTests
{
    internal class MemoryStoreStats : IStats
    {
        // Normally you'll want to .Stop() before accessing this.
        public List<Metric> Metrics
        {
            get
            {
                lock (_lock)
                {
                    return new List<Metric>(_metrics);    
                }
            }
        }

        private readonly object _lock = new object();
        private readonly List<Metric> _metrics = new List<Metric>();

        private DateTime _startTime = DateTime.UtcNow;
        private bool _isEnabled = true;

        public void Stop()
        {
            _isEnabled = false;
        }

        public void ClearAndStart()
        {
            _metrics.Clear();
            _startTime = DateTime.UtcNow;
            _isEnabled = true;
        }

        private double OffsetMillis()
        {
            return (DateTime.UtcNow - _startTime).TotalMilliseconds;
        }

        private void Store(string service, string state, object metric)
        {
            if (!_isEnabled) return;
            lock (_lock)
            {
                var m = (metric == null ? (float?) null : float.Parse(metric.ToString()));
                _metrics.Add(new Metric(OffsetMillis() / 1000, service, state, m));
            }
        }

        public void Event(string service, string state, long? metric = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, float? metric = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, double? metric = null)
        {
            Store(service, state, metric);
        }

        public void Elapsed(string service, string state, TimeSpan elapsed)
        {
            Store(service, state, elapsed.TotalMilliseconds);
        }

        public void Gauge(string service, string state, long? metric = null)
        {
            Store(service, state, metric);
        }

        public void ConfigGauge(string service, long metric)
        {
            Store(service, null, metric);
        }
    }
}