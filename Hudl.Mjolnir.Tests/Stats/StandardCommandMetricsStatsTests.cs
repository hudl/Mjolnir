using System;
using System.Threading.Tasks;
using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class StandardCommandMetricsStatsTests : TestFixture
    {
        [Fact]
        public async Task Construct_CreatesGauges()
        {
            var mockRiemann = new Mock<IRiemann>();
            var metrics = CreateMetrics("Test", mockRiemann);
            
            await Task.Delay(TimeSpan.FromMilliseconds(5010)); // TODO This is jank, but the interval's not injectable at the moment.

            mockRiemann.Verify(m => m.ConfigGauge("mjolnir metrics Test conf.windowMillis", It.IsAny<long>()));
            mockRiemann.Verify(m => m.ConfigGauge("mjolnir metrics Test conf.snapshotTtlMillis", It.IsAny<long>()));
        }

        [Fact]
        public void MarkCommandSuccess_Event()
        {
            var mockRiemann = new Mock<IRiemann>();
            var metrics = CreateMetrics("Test", mockRiemann);

            metrics.MarkCommandSuccess();

            mockRiemann.Verify(m => m.Event("mjolnir metrics Test Mark", "CommandSuccess", null, null, null, null), Times.Once);
        }

        [Fact]
        public void MarkCommandFailure_Event()
        {
            var mockRiemann = new Mock<IRiemann>();
            var metrics = CreateMetrics("Test", mockRiemann);

            metrics.MarkCommandFailure();

            mockRiemann.Verify(m => m.Event("mjolnir metrics Test Mark", "CommandFailure", null, null, null, null), Times.Once);
        }

        [Fact]
        public void GetSnapshot_NotCachedAndCached()
        {
            var mockRiemann = new Mock<IRiemann>();
            var clock = new ManualTestClock();
            var metrics = CreateMetrics("Test", mockRiemann, clock, 30000, 10000);

            clock.AddMilliseconds(10100); // Pass the snapshot TTL.

            metrics.GetSnapshot(); // Should create new snapshot.

            mockRiemann.Verify(m => m.Elapsed("mjolnir metrics Test CreateSnapshot", null, It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Elapsed("mjolnir metrics Test GetSnapshot", null, It.IsAny<TimeSpan>(), null, null, null), Times.Once);

            metrics.GetSnapshot(); // Should grab the cached one without re-creating.

            mockRiemann.Verify(m => m.Elapsed("mjolnir metrics Test CreateSnapshot", null, It.IsAny<TimeSpan>(), null, null, null), Times.Once); // Still once.
            mockRiemann.Verify(m => m.Elapsed("mjolnir metrics Test GetSnapshot", null, It.IsAny<TimeSpan>(), null, null, null), Times.Exactly(2)); // One more time.
        }

        [Fact]
        public void Reset_Elapsed()
        {
            var mockRiemann = new Mock<IRiemann>();
            var metrics = CreateMetrics("Test", mockRiemann);

            metrics.Reset();

            mockRiemann.Verify(m => m.Elapsed("mjolnir metrics Test Reset", null, It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        private static StandardCommandMetrics CreateMetrics(string key, IMock<IRiemann> mockRiemann, IClock clock = null,
            long? windowMillis = null, long? snapshotTtlMillis = null)
        {
            return new StandardCommandMetrics(
                GroupKey.Named(key),
                new TransientConfigurableValue<long>(windowMillis ?? 30000),
                new TransientConfigurableValue<long>(snapshotTtlMillis ?? 10000),
                (clock ?? new ManualTestClock()),
                mockRiemann.Object);
        }
    }
}
