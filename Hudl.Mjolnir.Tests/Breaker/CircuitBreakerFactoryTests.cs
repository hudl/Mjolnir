using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using Xunit;

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
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var groupKey = AnyGroupKey;
            var factory = new CircuitBreakerFactory(mockMetricEvents.Object, mockBreakerConfig.Object, mockLogFactory.Object);

            // Act

            var firstBreaker = factory.GetCircuitBreaker(groupKey);
            var secondBreaker = factory.GetCircuitBreaker(groupKey);

            // Assert

            Assert.True(firstBreaker == secondBreaker); // Reference should be equal for same object.
            Assert.True(firstBreaker.Metrics == secondBreaker.Metrics); // Inner metrics instances should also be the same.
        }
    }
}
