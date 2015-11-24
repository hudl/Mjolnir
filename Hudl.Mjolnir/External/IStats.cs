using System;

namespace Hudl.Mjolnir.External
{
    [Obsolete("Prefer IMetricEvents instead. IStats will likely be removed in a future major release.")]
    public interface IStats
    {
        void Event(string service, string state, long? metric);
        void Elapsed(string service, string state, TimeSpan elapsed);
        void Gauge(string service, string state, long? metric = null);
    }
}
