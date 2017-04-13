using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using System.Threading;
using Xunit;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class BulkheadFactoryTests : TestFixture
    {
        [Fact]
        public void Construct_InitializesConfigGauge_GaugeFiresForOneBulkhead()
        {
            // Arrange

            var key = AnyString;
            var groupKey = GroupKey.Named(key);
            var expectedMaxConcurrent = AnyPositiveInt;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadGauge(groupKey.Name, "semaphore", expectedMaxConcurrent, It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey)).Returns(expectedMaxConcurrent);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());
            
            // Act

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);
            
            // Add a bulkhead
            factory.GetBulkhead(groupKey);
            
            // The timer will fire after 1 second.
            Thread.Sleep(TimeSpan.FromMilliseconds(1010));

            // Assert

            // Gauges should fire every second, so wait one second and then verify.
            mockMetricEvents.Verify(m => m.BulkheadGauge(key, "semaphore", expectedMaxConcurrent, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void Construct_InitializesConfigGauge_GaugeFiresForMultipleBulkheads()
        {
            // Arrange

            var key1 = AnyString;
            var groupKey1 = GroupKey.Named(key1);

            var key2 = AnyString;
            var groupKey2 = GroupKey.Named(key2);

            var expectedMaxConcurrent1 = AnyPositiveInt;
            var expectedMaxConcurrent2 = AnyPositiveInt;
            
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadGauge(groupKey1.Name, "semaphore", expectedMaxConcurrent1, It.IsAny<int>()));
            mockMetricEvents.Setup(m => m.BulkheadGauge(groupKey2.Name, "semaphore", expectedMaxConcurrent2, It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey1)).Returns(expectedMaxConcurrent1);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey1, It.IsAny<Action<int>>()));
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey2)).Returns(expectedMaxConcurrent2);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey2, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            // Act + Assert

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Wait 2s - since we haven't yet created any bulkheads, we shouldn't have any events.

            mockMetricEvents.Verify(m => m.BulkheadGauge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);

            Thread.Sleep(TimeSpan.FromMilliseconds(1010));

            // Add two bulkheads
            factory.GetBulkhead(groupKey1);
            factory.GetBulkhead(groupKey2);

            Thread.Sleep(TimeSpan.FromMilliseconds(1010));
            
            mockMetricEvents.Verify(m => m.BulkheadGauge(key1, "semaphore", expectedMaxConcurrent1, It.IsAny<int>()), Times.Once);
            mockMetricEvents.Verify(m => m.BulkheadGauge(key2, "semaphore", expectedMaxConcurrent2, It.IsAny<int>()), Times.Once);
        }

        [Fact]
        public void GetBulkhead_ReturnsSameBulkheadForKey()
        {
            // Bulkheads are long-lived objects and used for many requests. In the absence
            // of any configuration changes, we should be using the same one through the
            // lifetime of the app.

            // Arrange

            var key = AnyGroupKey;

            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(AnyPositiveInt);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Act

            var bulkhead = factory.GetBulkhead(key);

            // Assert

            Assert.Equal(bulkhead, factory.GetBulkhead(key));
        }
        
        [Fact]
        public void GetBulkhead_ReturnsNewBulkheadWhenConfigChanges()
        {
            // Config can be used to resize the bulkhead at runtime, which results in a new
            // bulkhead being created.

            // To ensure consistency, callers who retrieve a bulkhead and call TryEnter()
            // on it should keep a local reference to the same bulkhead to later call
            // Release() on (rather than re-retrieving the bulkhead from the context).
            // That behavior is tested elsewhere (with the BulkheadInvoker tests).

            // Arrange

            var key = AnyString;
            var groupKey = GroupKey.Named(key);
            var configKey = $"mjolnir.bulkhead.{key}.maxConcurrent";
            const int initialExpectedCount = 5;
            const int newExpectedCount = 6;

            Action<int> changeHandler = null;

            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey)).Returns(initialExpectedCount);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey, It.IsAny<Action<int>>())).Callback((GroupKey gk, Action<int> occ) => changeHandler = occ);

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Act

            var firstBulkhead = factory.GetBulkhead(groupKey);
            changeHandler(newExpectedCount);

            // Give the change handler callback enough time to create and reassign the bulkhead.
            Thread.Sleep(500);

            var secondBulkhead = factory.GetBulkhead(groupKey);

            // Assert

            // Shouldn't change any existing referenced bulkheads...
            Assert.Equal(initialExpectedCount, firstBulkhead.CountAvailable);

            // ...but newly-retrieved bulkheads should get a new instance
            // with the updated count.
            Assert.Equal(newExpectedCount, secondBulkhead.CountAvailable);

            // And they shouldn't be the same bulkhead (which should be obvious by this point).
            Assert.False(firstBulkhead == secondBulkhead);
        }

        [Fact]
        public void GetBulkhead_WhenInitializingBulkheadAndMaxConcurrentConfigIsInvalid_Throws()
        {
            // Arrange

            var key = AnyGroupKey;
            const int invalidMaxConcurrent = -1;

            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(invalidMaxConcurrent);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Act + Assert

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => factory.GetBulkhead(key));
            
            Assert.Equal("maxConcurrent", exception.ParamName);
            Assert.Equal(invalidMaxConcurrent, exception.ActualValue);
        }

        [Fact]
        public void GetBulkhead_WhenInitializingBulkheadAndMaxConcurrentConfigIsInvalid_AndThenConfigChangedToValidValue_CreatesBulkhead()
        {
            // Arrange

            var key = AnyGroupKey;
            const int invalidMaxConcurrent = -1;
            const int validMaxConcurrent = 1;

            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            mockBulkheadConfig.SetupSequence(m => m.GetMaxConcurrent(key))
                .Returns(invalidMaxConcurrent)
                .Returns(validMaxConcurrent);
            
            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var factory = new BulkheadFactory(mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            try
            {
                factory.GetBulkhead(key);
            }
            catch(ArgumentOutOfRangeException)
            {
                // Expected, config is invalid for the first attempt.
            }

            // Act
            
            var bulkhead = factory.GetBulkhead(key); // Should not throw.

            // Assert

            Assert.Equal(validMaxConcurrent, bulkhead.CountAvailable);
        }
    }
}
