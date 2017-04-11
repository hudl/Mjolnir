using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using System.Threading;
using Xunit;
using static Hudl.Mjolnir.Bulkhead.BulkheadFactory;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class SemaphoreBulkheadHolderTests : TestFixture
    {
        [Fact]
        public void Construct_ThrowsIfNullMetricEvents()
        {
            // Arrange

            var key = AnyGroupKey;
            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);

            // Act + Assert

            var exception = Assert.Throws<ArgumentNullException>(() => new SemaphoreBulkheadHolder(key, null, mockBulkheadConfig.Object, mockLogFactory.Object));
            Assert.Equal("metricEvents", exception.ParamName);
        }

        [Fact]
        public void Construct_ThrowsIfNullConfig()
        {
            // Arrange

            var key = AnyGroupKey;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);

            // Act + Assert

            var exception = Assert.Throws<ArgumentNullException>(() => new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, null, mockLogFactory.Object));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void Construct_ThrowsIfNullLogFactory()
        {
            // Arrange

            var key = AnyGroupKey;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);

            // Act + Assert

            var exception = Assert.Throws<ArgumentNullException>(() => new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, mockBulkheadConfig.Object, null));
            Assert.Equal("logFactory", exception.ParamName);
        }

        [Fact]
        public void Construct_SetsInitialBulkhead()
        {
            // Arrange

            var key = AnyGroupKey;
            var expectedMaxConcurrent = AnyPositiveInt;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadConfigGauge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(expectedMaxConcurrent);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            // Act

            var holder = new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Assert

            Assert.Equal(key.Name, holder.Bulkhead.Name);
            Assert.Equal(expectedMaxConcurrent, holder.Bulkhead.CountAvailable);
        }

        [Fact]
        public void Construct_CallsAddChangeHandler()
        {
            // Arrange

            var key = AnyGroupKey;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadConfigGauge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(AnyPositiveInt);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            // Act

            var holder = new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Assert

            // It.IsAny<Action<int>>() could be better here, but I can't access the non-static method group.
            mockBulkheadConfig.Verify(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()), Times.Once);
        }

        [Fact]
        public void Construct_InitializesConfigGauge()
        {
            // Arrange

            var key = AnyString;
            var groupKey = GroupKey.Named(key);
            var expectedMaxConcurrent = AnyPositiveInt;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadConfigGauge(groupKey.Name, "semaphore", expectedMaxConcurrent));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey)).Returns(expectedMaxConcurrent);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            // Act

            var holder = new SemaphoreBulkheadHolder(groupKey, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);
            Thread.Sleep(TimeSpan.FromMilliseconds(1100));

            // Assert

            // Gauges should fire every second, so wait one second and then verify.
            mockMetricEvents.Verify(m => m.BulkheadConfigGauge(key, "semaphore", expectedMaxConcurrent), Times.Once);

        }

        [Fact]
        public void Construct_WhenMaxConcurrentConfigIsInvalid_DoesSomething()
        {
            // Arrange

            var key = AnyString;
            var groupKey = GroupKey.Named(key);
            const int invalidMaxConcurrent = -1;
            var mockMetricEvents = new Mock<IMetricEvents>(); // Not Strict: we're not testing the events here.

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(groupKey)).Returns(invalidMaxConcurrent);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(groupKey, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            // Act + Assert

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SemaphoreBulkheadHolder(groupKey, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object));
            
            Assert.Equal("maxConcurrent", exception.ParamName);
            Assert.Equal(invalidMaxConcurrent, exception.ActualValue);
        }

        [Fact]
        public void UpdateMaxConcurrent_IgnoresInvalidValues()
        {
            // Arrange

            var key = AnyGroupKey;
            const int invalidMaxConcurrent = -1;

            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadConfigGauge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(AnyPositiveInt);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));
            mockBulkheadConfig.Setup(m => m.GetConfigKey(key)).Returns(key.Name);

            var mockLog = new Mock<IMjolnirLog>(MockBehavior.Strict);
            mockLog.Setup(m => m.Error(It.IsAny<string>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(mockLog.Object);

            var holder = new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Act

            var initialBulkhead = holder.Bulkhead;
            holder.UpdateMaxConcurrent(invalidMaxConcurrent);

            // Assert

            // Bulkhead should be unchanged.
            Assert.True(initialBulkhead == holder.Bulkhead);
            mockLog.Verify(m => m.Error($"Semaphore bulkhead config {key.Name} changed to an invalid limit of {invalidMaxConcurrent}, the bulkhead will not be changed"), Times.Once);
        }

        [Fact]
        public void UpdateMaxConcurrent_ReplacesBulkhead()
        {
            // Arrange
            
            var key = AnyGroupKey;
            const int initialExpectedCount = 5;
            const int newExpectedCount = 6;
            var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
            mockMetricEvents.Setup(m => m.BulkheadConfigGauge(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()));

            var mockBulkheadConfig = new Mock<IBulkheadConfig>(MockBehavior.Strict);
            mockBulkheadConfig.Setup(m => m.GetMaxConcurrent(key)).Returns(initialExpectedCount);
            mockBulkheadConfig.Setup(m => m.AddChangeHandler(key, It.IsAny<Action<int>>()));

            var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
            mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

            var holder = new SemaphoreBulkheadHolder(key, mockMetricEvents.Object, mockBulkheadConfig.Object, mockLogFactory.Object);

            // Act

            var firstBulkhead = holder.Bulkhead;
            holder.UpdateMaxConcurrent(newExpectedCount);

            var secondBulkhead = holder.Bulkhead;

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
