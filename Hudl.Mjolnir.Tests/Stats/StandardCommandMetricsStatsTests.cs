using System;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class StandardCommandMetricsStatsTests : TestFixture
    {
        [Fact]
        public void MarkCommandSuccess_Event()
        {
            var mockStats = new Mock<IStats>();
            var metrics = CreateMetrics("Test", mockStats);

            metrics.MarkCommandSuccess();

            mockStats.Verify(m => m.Event("mjolnir metrics Test Mark", "CommandSuccess", null), Times.Once);
        }

        [Fact]
        public void MarkCommandFailure_Event()
        {
            var mockStats = new Mock<IStats>();
            var metrics = CreateMetrics("Test", mockStats);

            metrics.MarkCommandFailure();

            mockStats.Verify(m => m.Event("mjolnir metrics Test Mark", "CommandFailure", null), Times.Once);
        }

        [Fact]
        public void GetSnapshot_NotCachedAndCached()
        {
            var mockStats = new Mock<IStats>();
            var clock = new ManualTestClock();
            var metrics = CreateMetrics("Test", mockStats, clock, 30000, 10000);

            clock.AddMilliseconds(10100); // Pass the snapshot TTL.

            metrics.GetSnapshot(); // Should create new snapshot.

            mockStats.Verify(m => m.Elapsed("mjolnir metrics Test CreateSnapshot", null, It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir metrics Test GetSnapshot", null, It.IsAny<TimeSpan>()), Times.Once);

            metrics.GetSnapshot(); // Should grab the cached one without re-creating.

            mockStats.Verify(m => m.Elapsed("mjolnir metrics Test CreateSnapshot", null, It.IsAny<TimeSpan>()), Times.Once); // Still once.
            mockStats.Verify(m => m.Elapsed("mjolnir metrics Test GetSnapshot", null, It.IsAny<TimeSpan>()), Times.Exactly(2)); // One more time.
        }

        [Fact]
        public void Reset_Elapsed()
        {
            var mockStats = new Mock<IStats>();
            var metrics = CreateMetrics("Test", mockStats);

            metrics.Reset();

            mockStats.Verify(m => m.Elapsed("mjolnir metrics Test Reset", null, It.IsAny<TimeSpan>()), Times.Once);
        }

        private static StandardCommandMetrics CreateMetrics(string key, IMock<IStats> mockStats, IClock clock = null,
            long? windowMillis = null, long? snapshotTtlMillis = null)
        {
            return new StandardCommandMetrics(
                GroupKey.Named(key),
                new TransientConfigurableValue<long>(windowMillis ?? 30000),
                new TransientConfigurableValue<long>(snapshotTtlMillis ?? 10000),
                (clock ?? new ManualTestClock()),
                mockStats.Object);
        }
    }
}
