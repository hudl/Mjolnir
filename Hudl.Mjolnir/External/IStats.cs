using System;

namespace Hudl.Mjolnir.External
{
    public interface IStats
    {
        void Event(string service, string state, float? metric);
        void Elapsed(string service, string state, TimeSpan elapsed);
        void Gauge(string service, string state, long? metric = null);
        void ConfigGauge(string service, long metric);
    }
}
