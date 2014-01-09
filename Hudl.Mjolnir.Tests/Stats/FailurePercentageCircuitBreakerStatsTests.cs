using System;
using System.Threading.Tasks;
using Hudl.Common.Clock;
using Hudl.Mjolnir.Tests.Breaker;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class FailurePercentageCircuitBreakerStatsTests
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            await Task.Delay(TimeSpan.FromMilliseconds(5010)); // TODO This is jank, but the interval's not injectable at the moment.
            
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir breaker Test conf.minimumOperations", It.IsAny<long>()), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir breaker Test conf.thresholdPercentage", It.IsAny<long>()), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir breaker Test conf.trippedDurationMillis", It.IsAny<long>()), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir breaker Test conf.forceTripped", It.IsAny<long>()), Times.Once);
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir breaker Test conf.forceFixed", It.IsAny<long>()), Times.Once);

            mockRiemann.Verify(m => m.Gauge("mjolnir breaker Test total", It.IsIn("Above", "Below"), It.IsAny<long>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Gauge("mjolnir breaker Test error", It.IsIn("Above", "Below"), It.IsAny<int>(), null, null, null), Times.Once);
        }

        [Fact]
        public void IsAllowing_Allows()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Allowed", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void IsAllowing_ForceTripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();
            breaker.Properties.ForceTripped.Value = true;

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void IsAllowing_ForceFixed()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 50, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();
            breaker.Properties.ForceFixed.Value = true;

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Allowed", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void IsAllowing_Tripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            
            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test IsAllowing", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Exactly(2));
        }

        [Fact]
        public void AllowSingleTest_TrippedAndNotPastWaitDuration()
        {
            var mockRiemann = new Mock<IRiemann>();
            var clock = new ManualTestClock();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .WithClock(clock)
                .WithWaitMillis(1000)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            // Don't advance the clock.

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>(), null, null, null), Times.Exactly(2));
        }

        [Fact]
        public void AllowSingleTest_TrippedAndPastWaitDuration()
        {
            var mockRiemann = new Mock<IRiemann>();
            var clock = new ManualTestClock();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .WithClock(clock)
                .WithWaitMillis(1000)
                .Create();
            breaker.IsAllowing(); // Trip.
            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "NotEligible", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            clock.AddMilliseconds(2000); // Advance past wait duration.

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", "Allowed", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void AllowSingleTest_NotTripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 100, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test AllowSingleTest", It.IsAny<string>(), It.IsAny<TimeSpan>(), null, null, null), Times.Never);
        }

        [Fact]
        public void CheckAndSetTripped_TotalBelowMinimum()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(10, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "CriteriaNotMet", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_ErrorBelowThreshold()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 100, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "CriteriaNotMet", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_AlreadyTripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();
            breaker.IsAllowing(); // Trip.
            
            breaker.IsAllowing();

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "AlreadyTripped", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public void CheckAndSetTripped_JustTripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.IsAllowing(); // Trip.

            mockRiemann.Verify(m => m.Elapsed("mjolnir breaker Test CheckAndSetTripped", "JustTripped", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Event("mjolnir breaker Test", "Tripped", null, null, null, null));
        }

        [Fact]
        public void MarkSuccess_NotTripped()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();

            breaker.MarkSuccess(0);

            mockRiemann.Verify(m => m.Event("mjolnir breaker Test MarkSuccess", "Ignored", null, null, null, null), Times.Once);
        }

        [Fact]
        public void MarkSuccess_Fixed()
        {
            var mockRiemann = new Mock<IRiemann>();
            var breaker = new BreakerBuilder(0, 0, "Test")
                .WithRiemann(mockRiemann.Object)
                .Create();
            breaker.IsAllowing(); // Trip.

            breaker.MarkSuccess(0);

            mockRiemann.Verify(m => m.Event("mjolnir breaker Test MarkSuccess", "Fixed", null, null, null, null), Times.Once);
        }
    }
}
