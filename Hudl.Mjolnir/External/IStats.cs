using System;

namespace Hudl.Mjolnir.External
{
    public interface IStats
    {
        void Event(string service, string state, long? metric);
        void Elapsed(string service, string state, TimeSpan elapsed);
        void Gauge(string service, string state, long? metric = null);
    }

    
}
