using Hudl.Mjolnir.Clock;
using System;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class ManualTestClock : IClock
    {
        private long _currentMillis;

        public long GetMillisecondTimestamp()
        {
            return _currentMillis;
        }

        public void AddMilliseconds(long milliseconds)
        {
            if (milliseconds < 0)
            {
                throw new ArgumentException("Great Scott!");
            }
            _currentMillis += milliseconds;
        }
    }
}
