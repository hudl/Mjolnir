using System;

namespace Hudl.Mjolnir.Clock
{
    /// <summary>
    /// Returns the current system time, UTC.
    /// </summary>
    internal class UtcSystemClock : IClock
    {
        public long GetMillisecondTimestamp()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}
