using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
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

            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetWindowMillis(It.IsAny<GroupKey>())).Returns(periodMillis);
            
            var clock = new ManualTestClock();
            var bucket = new ResettingNumbersBucket(AnyGroupKey, clock, mockConfig.Object, new DefaultMjolnirLogFactory());

            bucket.Increment(CounterMetric.CommandSuccess);

            clock.AddMilliseconds(periodMillis + 1);
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess));

            bucket.Increment(CounterMetric.CommandSuccess); // Should reset and then count one.
            Assert.Equal(1, bucket.GetCount(CounterMetric.CommandSuccess)); // Should be 1, not 2.
        }

        private ResettingNumbersBucket CreateBucket()
        {
            var mockConfig = new Mock<IFailurePercentageCircuitBreakerConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetWindowMillis(It.IsAny<GroupKey>())).Returns(10000);
            
            var clock = new ManualTestClock();
            return new ResettingNumbersBucket(AnyGroupKey, clock, mockConfig.Object, new DefaultMjolnirLogFactory());
        }
    }
}
