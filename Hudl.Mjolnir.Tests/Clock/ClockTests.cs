using Hudl.Mjolnir.Clock;
using Hudl.Mjolnir.Tests.Helper;
using System;
using System.Threading;
using Xunit;

namespace Hudl.Mjolnir.Tests.Clock
{
    public class ClockTests
    {
        [Fact]
        public void SystemClock_GetMillisecondTimestamp_IsCloseToUtcNow()
        {
            const long epsilonMillis = 10;
            var clock = new SystemClock();
            var now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            // Just make sure they're close.
            var difference = Math.Abs(clock.GetMillisecondTimestamp() - now);
            Assert.True(difference < epsilonMillis, "SystemClock difference (" + difference + ") exceeded epsilon (" + epsilonMillis + ")");
        }
        
        [Fact]
        public void ManualTestClock_GetMillisecondTimestamp_StartsAtZero()
        {
            var clock = new ManualTestClock();
            Assert.Equal(0, clock.GetMillisecondTimestamp());
        }

        [Fact]
        public void ManualTestClock_GetMillisecondTimestamp_DoesntAdvanceAutomatically()
        {
            var clock = new ManualTestClock();
            Thread.Sleep(10);
            Assert.Equal(0, clock.GetMillisecondTimestamp());
        }

        [Fact]
        public void ManualTestClock_GetMillisecondTimestamp_UpdatesAfterAddingMillis()
        {
            var clock = new ManualTestClock();
            clock.AddMilliseconds(20);
            Assert.Equal(20, clock.GetMillisecondTimestamp());
        }

        [Fact]
        public void ManualTestClock_AddMilliseconds_DisallowsNegatives()
        {
            var clock = new ManualTestClock();
            Assert.Throws<ArgumentException>(() => clock.AddMilliseconds(-10));
        }
    }
}