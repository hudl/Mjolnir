using System;
using System.Collections.Generic;
using Hudl.Riemann;

namespace Hudl.Mjolnir.SystemTests
{
    internal class MemoryStoreRiemann : IRiemann
    {
        //private static readonly ILog Log = LogManager.GetLogger("metrics");
        //private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        public void Event(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, float? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Event(string service, string state, double? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void Elapsed(string service, string state, TimeSpan elapsed, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, elapsed.TotalMilliseconds);
        }

        public void Gauge(string service, string state, long? metric = null, ISet<string> tags = null, string description = null, int? ttl = null)
        {
            Store(service, state, metric);
        }

        public void ConfigGauge(string service, long metric)
        {
            Store(service, null, metric);
        }
    }
}