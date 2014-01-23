using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Metrics
{
    public class StandardCommandMetricsTests : TestFixture
    {
        [Fact]
        public void MarkCommandSuccess_BeforeFirstSnapshot_GetsIncludedInSnapshot()
        {
            var metrics = new StandardCommandMetrics(GroupKey.Named("Test"), new TransientConfigurableValue<long>(30000), new TransientConfigurableValue<long>(1000));
            metrics.MarkCommandSuccess();

            var snapshot = metrics.GetSnapshot();
            Assert.Equal(1, snapshot.Total);
            Assert.Equal(0, snapshot.ErrorPercentage);
        }

        [Fact]
        public void MarkCommandFailure_BeforeFirstSnapshot_GetsIncludedInSnapshot()
        {
            var metrics = new StandardCommandMetrics(GroupKey.Named("Test"), new TransientConfigurableValue<long>(30000), new TransientConfigurableValue<long>(1000));
            metrics.MarkCommandFailure();

            var snapshot = metrics.GetSnapshot();
            Assert.Equal(1, snapshot.Total);
            Assert.Equal(100, snapshot.ErrorPercentage);
        }

        [Fact]
        public void GetSnapshot_VariousPercentageTests()
        {
            Assert.Equal(0, SnapshotFor(0, 0).Total);
            Assert.Equal(1, SnapshotFor(0, 1).Total);
            Assert.Equal(1, SnapshotFor(1, 0).Total);
            Assert.Equal(2, SnapshotFor(1, 1).Total);

            // Potential divide-by-zero cases.
            Assert.Equal(100, SnapshotFor(0, 1).ErrorPercentage);
            Assert.Equal(0, SnapshotFor(1, 0).ErrorPercentage);
            Assert.Equal(0, SnapshotFor(0, 0).ErrorPercentage);

            // Middle, boundary values.
            Assert.Equal(50, SnapshotFor(1, 1).ErrorPercentage);
            Assert.Equal(99, SnapshotFor(1, 99).ErrorPercentage);
            Assert.Equal(1, SnapshotFor(99, 1).ErrorPercentage);

            // Rounding (should round down, i.e. truncate).
            Assert.Equal(50, SnapshotFor(499, 501).ErrorPercentage);
        }

        [Fact]
        public void GetSnapshot_WithinCachePeriod_ReturnsPreviousSnapshot()
        {
            var clock = new ManualTestClock();

            // Within the metrics, the last snapshot timestamp will probably be zero.
            // Let's start our clock with something far away from zero.
            clock.AddMilliseconds(new SystemClock().GetMillisecondTimestamp());
            var metrics = new StandardCommandMetrics(GroupKey.Named("Test"), new TransientConfigurableValue<long>(10000), new TransientConfigurableValue<long>(1000), clock);

            metrics.MarkCommandSuccess();
            metrics.GetSnapshot(); // Take the first snapshot to cache it.
            metrics.MarkCommandSuccess();
            clock.AddMilliseconds(500); // Still within the snapshot TTL (1000).
            Assert.Equal(1, metrics.GetSnapshot().Total);
            clock.AddMilliseconds(1000); // Push time past the TTL.
            Assert.Equal(2, metrics.GetSnapshot().Total);
        }

        private MetricsSnapshot SnapshotFor(int success, int failure)
        {
            var metrics = new StandardCommandMetrics(GroupKey.Named("Test"), new TransientConfigurableValue<long>(10000), new TransientConfigurableValue<long>(0)); // Don't cache snapshots.
            for (var i = 0; i < success; i++)
            {
                metrics.MarkCommandSuccess();
            }

            for (var i = 0; i < failure; i++)
            {
                metrics.MarkCommandFailure();
            }
            return metrics.GetSnapshot();
        }
    }
}
