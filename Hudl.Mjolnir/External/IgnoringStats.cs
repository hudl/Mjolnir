using System;

namespace Hudl.Mjolnir.External
{
    internal class IgnoringStats : IStats
    {
        public void Event(string service, string state, long? metric) {}
        public void Elapsed(string service, string state, TimeSpan elapsed) {}
        public void Gauge(string service, string state, long? metric = null) {}
    }
}
