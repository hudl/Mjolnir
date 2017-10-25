using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using System.Threading;
using Xunit;
using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Metrics;

namespace Hudl.Mjolnir.Tests.Breaker
{
    public class CircuitBreakerFactoryTests : TestFixture
    {
        [Fact]
        public void GetCircuitBreaker_ReturnsSameObjectForSameKey()
        {
            // Arrange

            var mockMetricEvents = new Mock<IMetricEvents>();
            var mockBreakerConfig = new Mock<IFailurePercentageCircuitBreakerConfig>(MockBehavior.Strict);
            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog<BulkheadFactory>()).Returns(new DefaultMjolnirLog<BulkheadFactory>());
            mockLogFactory.Setup(m => m.CreateLog<FailurePercentageCircuitBreaker>()).Returns(new DefaultMjolnirLog<FailurePercentageCircuitBreaker>());
            mockLogFactory.Setup(m => m.CreateLog<ResettingNumbersBucket>()).Returns(new DefaultMjolnirLog<ResettingNumbersBucket>());
            var groupKey = AnyGroupKey;
            var factory = new CircuitBreakerFactory(mockMetricEvents.Object, mockBreakerConfig.Object, mockLogFactory.Object);

            // Act

            var firstBreaker = factory.GetCircuitBreaker(groupKey);
            var secondBreaker = factory.GetCircuitBreaker(groupKey);

            // Assert

            Assert.True(firstBreaker == secondBreaker); // Reference should be equal for same object.
            Assert.True(firstBreaker.Metrics == secondBreaker.Metrics); // Inner metrics instances should also be the same.
        }

        [Fact]
        public void Construct_InitializesConfigGauge_GaugeFiresForOneBulkhead()
        {
            // Arrange

            var key = AnyString;
            var groupKey = GroupKey.Named(key);

            var expectedMinimumOperations = AnyPositiveInt;
            var expectedWindowMillis = AnyPositiveInt;
            var expectedThresholdPercent = AnyPositiveInt;
            var expectedTrippedDurationMillis = AnyPositiveInt;
            var expectedForceTripped = false;
            var expectedForceFixed = false;

            var mockMetricEvents = new Mock<IMetricEvents>();

            var mockBreakerConfig = new Mock<IFailurePercentageCircuitBreakerConfig>(MockBehavior.Strict);
            mockBreakerConfig.Setup(m => m.GetMinimumOperations(groupKey)).Returns(expectedMinimumOperations);
            mockBreakerConfig.Setup(m => m.GetWindowMillis(groupKey)).Returns(expectedWindowMillis);
            mockBreakerConfig.Setup(m => m.GetThresholdPercentage(groupKey)).Returns(expectedThresholdPercent);
            mockBreakerConfig.Setup(m => m.GetTrippedDurationMillis(groupKey)).Returns(expectedTrippedDurationMillis);
            mockBreakerConfig.Setup(m => m.GetForceTripped(groupKey)).Returns(expectedForceTripped);
            mockBreakerConfig.Setup(m => m.GetForceFixed(groupKey)).Returns(expectedForceFixed);

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog<FailurePercentageCircuitBreaker>()).Returns(new DefaultMjolnirLog<FailurePercentageCircuitBreaker>());
            mockLogFactory.Setup(m => m.CreateLog<ResettingNumbersBucket>()).Returns(new DefaultMjolnirLog<ResettingNumbersBucket>());
            // Act + Assert

            var factory = new CircuitBreakerFactory(mockMetricEvents.Object, mockBreakerConfig.Object, mockLogFactory.Object);

            // Gauge won't fire immediately, it'll start one second after construction
            mockMetricEvents.Verify(m => m.BreakerGauge(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never);

            // Add two breakers
            var firstBreaker = factory.GetCircuitBreaker(groupKey);

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            mockMetricEvents.Verify(m => m.BreakerGauge(key, expectedMinimumOperations, expectedWindowMillis, expectedThresholdPercent, expectedTrippedDurationMillis, expectedForceTripped, expectedForceFixed, false, 0, 0));
        }
    }
}
