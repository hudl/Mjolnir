using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Log;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using Xunit;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class BulkheadFactoryTests
    {
        public class GetBulkhead
        {
            [Fact]
            public void ReturnsSameBulkheadForKey()
            {
                // Bulkheads are long-lived objects and used for many requests. In the absence
                // of any configuration changes, we should be using the same one through the
                // lifetime of the app.

                var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
                var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
                mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

                var bulkheadConfig = new BulkheadConfig(new DefaultValueConfig());

                var key = GroupKey.Named(Rand.String());
                var factory = new BulkheadFactory(mockMetricEvents.Object, bulkheadConfig, mockLogFactory.Object);

                var bulkhead = factory.GetBulkhead(key);
                Assert.Equal(bulkhead, factory.GetBulkhead(key));
            }

            [Fact]
            public void DefaultBulkheadSizeIs10()
            {
                // This is mainly to ensure we don't introduce a breaking change for clients
                // who rely on the default configs. 
                
                var mockMetricEvents = new Mock<IMetricEvents>(MockBehavior.Strict);
                var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
                mockLogFactory.Setup(m => m.CreateLog(It.IsAny<Type>())).Returns(new DefaultMjolnirLog());

                var bulkheadConfig = new BulkheadConfig(new DefaultValueConfig());

                var key = GroupKey.Named(Rand.String());
                var factory = new BulkheadFactory(mockMetricEvents.Object, bulkheadConfig, mockLogFactory.Object);

                var bulkhead = factory.GetBulkhead(key);
                Assert.Equal(10, bulkhead.CountAvailable);
            }

            // TODO rewrite this test now that config is mock-able and change handlers have changed
            //[Fact]
            //public void ReturnsNewBulkheadWhenConfigChanges()
            //{
            //    // Config can be used to resize the bulkhead at runtime, which results in a new
            //    // bulkhead being created.

            //    // To ensure consistency, callers who retrieve a bulkhead and call TryEnter()
            //    // on it should keep a local reference to the same bulkhead to later call
            //    // Release() on (rather than re-retrieving the bulkhead from the context).
            //    // That behavior is tested elsewhere (with the BulkheadInvoker tests).

            //    var key = Rand.String();
            //    var groupKey = GroupKey.Named(key);
            //    var configKey = "mjolnir.bulkhead." + key + ".maxConcurrent";
            //    var context = new CommandContextImpl();

            //    int initialExpectedCount = 5;

            //    ConfigProvider.Instance.Set(configKey, initialExpectedCount);

            //    var bulkhead = context.GetBulkhead(groupKey);

            //    Assert.Equal(5, initialExpectedCount);

            //    // Now, change the config value and make sure it gets applied.

            //    int newExpectedCount = 2;
            //    ConfigProvider.Instance.Set(configKey, newExpectedCount);

            //    // Shouldn't change any existing referenced bulkheads...
            //    Assert.Equal(initialExpectedCount, bulkhead.CountAvailable);

            //    // ...but newly-retrieved bulkheads should get a new instance
            //    // with the updated count.
            //    var newBulkhead = context.GetBulkhead(groupKey);
            //    Assert.NotEqual(bulkhead, newBulkhead);
            //    Assert.Equal(newExpectedCount, newBulkhead.CountAvailable);
            //}

            // TODO rewrite this test now that config is mock-able and change handlers have changed
            //[Fact]
            //public void IgnoresConfigChangeToInvalidValues()
            //{
            //    var key = Rand.String();
            //    var groupKey = GroupKey.Named(key);
            //    var configKey = "mjolnir.bulkhead." + key + ".maxConcurrent";
            //    var context = new CommandContextImpl();

            //    // Should have a valid default value initially.
            //    Assert.Equal(10, context.GetBulkhead(groupKey).CountAvailable);

            //    // Negative limits aren't valid.
            //    ConfigProvider.Instance.Set(configKey, -1);
            //    Assert.Equal(10, context.GetBulkhead(groupKey).CountAvailable);

            //    // Zero (disallow all) is a valid value.
            //    ConfigProvider.Instance.Set(configKey, 0);
            //    Assert.Equal(0, context.GetBulkhead(groupKey).CountAvailable);
            //}
        }
    }
}
