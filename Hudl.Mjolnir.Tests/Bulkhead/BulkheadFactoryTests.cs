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

        // TODO how to assert the assumption that callers will keep a reference to the bulkead they use, even if config changes?

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
    }
}
