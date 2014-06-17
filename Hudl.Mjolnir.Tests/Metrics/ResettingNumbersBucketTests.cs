using Hudl.Common.Clock;
using Hudl.Config;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Metrics
{
    public class ResettingNumbersBucketTests : TestFixture
    {
        [Fact]
        public void Construct_StartsWithZeroMetrics()
        {
            var bucket = CreateBucket();

            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandSuccess));
            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandFailure));
        }

        [Fact]
        public void Increment_Increments()
        {
            var bucket = CreateBucket();

            bucket.Increment(CounterMetric.CommandSuccess);
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess));
        }

        [Fact]
        public void Increment_DoesntIncrementOtherMetrics()
        {
            var bucket = CreateBucket();

            bucket.Increment(CounterMetric.CommandSuccess);
            Assert.Equal(0, bucket.GetCount(CounterMetric.CommandFailure));
        }

        [Fact]
        public void Increment_AfterPeriodExceeded_ResetsBeforeIncrementing()
        {
            const long periodMillis = 1000;
            var clock = new ManualTestClock();
            var bucket = new ResettingNumbersBucket(clock, new TransientConfigurableValue<long>(periodMillis));

            bucket.Increment(CounterMetric.CommandSuccess);

            clock.AddMilliseconds(periodMillis + 1);
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess));

            bucket.Increment(CounterMetric.CommandSuccess); // Should reset and then count one.
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess)); // Should be 1, not 2.
        }

        private ResettingNumbersBucket CreateBucket()
        {
            var clock = new ManualTestClock();
            return new ResettingNumbersBucket(clock, new TransientConfigurableValue<long>(10000));
        }
    }
}
