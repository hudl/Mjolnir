using System;
using System.Threading.Tasks;
using Hudl.Common.Clock;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Breaker;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class FailurePercentageCircuitBreakerStatsTests : TestFixture
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            const long gaugeIntervalMillis = 50;

            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithStats(mockStats.Object)
                .WithGaugeIntervalOverride(gaugeIntervalMillis)
                .Create();

            await Task.Delay(TimeSpan.FromMilliseconds(gaugeIntervalMillis + 50));
            
            mockStats.Verify(m => m.Gauge("mjolnir breaker Test total", It.IsIn("Above", "Below"), It.IsAny<long>()), Times.AtLeastOnce);
            mockStats.Verify(m => m.Gauge("mjolnir breaker Test error", It.IsIn("Above", "Below"), It.IsAny<int>()), Times.AtLeastOnce);
        }

        [Fact]
        public void IsAllowing_Allows()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithStats(mockStats.Object)
                .Create();

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Allowed", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void IsAllowing_ForceTripped()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithStats(mockStats.Object)
                .Create();
            breaker.Properties.ForceTripped.Value = true;

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void IsAllowing_ForceFixed()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithStats(mockStats.Object)
                .Create();
            breaker.Properties.ForceFixed.Value = true;

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Allowed", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void IsAllowing_Tripped()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>()), Times.Once);
            
            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>()), Times.Exactly(2));
        }

        [Fact]
        public void AllowSingleTest_TrippedAndNotPastWaitDuration()
        {
            var mockStats = new Mock<IStats>();
            var clock = new ManualTestClock();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .WithClock(clock)
                .WithWaitMillis(1000)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>()), Times.Once);
            // Don't advance the clock.

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>()), Times.Exactly(2));
        }

        [Fact]
        public void AllowSingleTest_TrippedAndPastWaitDuration()
        {
            var mockStats = new Mock<IStats>();
            var clock = new ManualTestClock();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .WithClock(clock)
                .WithWaitMillis(1000)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>()), Times.Once);
            clock.AddMilliseconds(2000); // Advance past wait duration.

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "Allowed", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void AllowSingleTest_NotTripped()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 100, "Test")
                .WithStats(mockStats.Object)
                .Create();

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public void CheckAndSetTripped_TotalBelowMinimum()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(10, 0, "Test")
                .WithStats(mockStats.Object)
                .Create();

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "CriteriaNotMet", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_ErrorBelowThreshold()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(0, 100, "Test")
                .WithStats(mockStats.Object)
                .Create();

            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "CriteriaNotMet", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_AlreadyTripped()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .Create();
            breaker.IsAllowing(); // Trip.
            
            breaker.IsAllowing();

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "AlreadyTripped", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_JustTripped()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .WithMetricEvents(mockMetricEvents.Object)
                .Create();

            breaker.IsAllowing(); // Trip.

            mockStats.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "JustTripped", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Event("mjolnir breaker Test", "Tripped", null));
            mockMetricEvents.Verify(m => m.BreakerTripped("Test"));
        }

        [Fact]
        public void MarkSuccess_NotTripped()
        {
            var mockStats = new Mock<IStats>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .Create();

            breaker.MarkSuccess(0);

            mockStats.Verify(m => m.Event("mjolnir breaker Test MarkSuccess", "Ignored", null), Times.Once);
        }

        [Fact]
        public void MarkSuccess_Fixed()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithStats(mockStats.Object)
                .WithMetricEvents(mockMetricEvents.Object)
                .Create();
            breaker.IsAllowing(); // Trip.

            breaker.MarkSuccess(0);

            mockStats.Verify(m => m.Event("mjolnir breaker Test MarkSuccess", "Fixed", null), Times.Once);
            mockMetricEvents.Verify(m => m.BreakerFixed("Test"));
        }
    }
}
