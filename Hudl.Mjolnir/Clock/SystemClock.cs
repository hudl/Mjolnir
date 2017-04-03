using System;

namespace Hudl.Mjolnir.Clock
{
    internal class SystemClock : IClock
    {
        public long GetMillisecondTimestamp()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
