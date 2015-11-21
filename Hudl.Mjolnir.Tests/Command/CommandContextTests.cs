using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandContextTests
    {
        public class GetBulkhead : TestFixture
        {
            [Fact]
            public void ReturnsSameBulkheadForKey()
            {
                // Bulkheads are long-lived objects and used for many requests. In the absence
                // of any configuration changes, we should be using the same one through the
                // lifetime of the app.

                var key = GroupKey.Named(Rand.String());
                var context = new CommandContextImpl();

                var bulkhead = context.GetBulkhead(key);
                Assert.Equal(bulkhead, context.GetBulkhead(key));
            }

            [Fact]
            public void DefaultBulkheadSizeIs10()
            {
                // This is mainly to ensure we don't introduce a breaking change for clients
                // who rely on the default configs. 

                var key = GroupKey.Named(Rand.String());
                var context = new CommandContextImpl();

                var bulkhead = context.GetBulkhead(key);
                Assert.Equal(10, bulkhead.Available);
            }

            [Fact]
            public void ReturnsNewBulkheadWhenConfigChanges()
            {
                // Config can be used to resize the bulkhead at runtime, which results in a new
                // bulkhead being created.

                // To ensure consistency, callers who retrieve a bulkhead and call TryEnter()
                // on it should keep a local reference to the same bulkhead to later call
                // Release() on (rather than re-retrieving the bulkhead from the context).
                // That behavior is tested elsewhere (with the BulkheadInvoker tests).

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                var configKey = "mjolnir.bulkheads." + key + ".maxConcurrent";
                var context = new CommandContextImpl();

                int initialExpectedCount = 5;

                ConfigProvider.Instance.Set(configKey, initialExpectedCount);

                var bulkhead = context.GetBulkhead(groupKey);

                Assert.Equal(5, initialExpectedCount);
                
                // Now, change the config value and make sure it gets applied.

                int newExpectedCount = 2;
                ConfigProvider.Instance.Set(configKey, newExpectedCount);

                // Shouldn't change any existing referenced bulkheads...
                Assert.Equal(initialExpectedCount, bulkhead.Available);

                // ...but newly-retrieved bulkheads should get a new instance
                // with the updated count.
                var newBulkhead = context.GetBulkhead(groupKey);
                Assert.NotEqual(bulkhead, newBulkhead);
                Assert.Equal(newExpectedCount, newBulkhead.Available);
            }

            [Fact]
            public void IgnoresConfigChangeToInvalidValues()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                var configKey = "mjolnir.bulkheads." + key + ".maxConcurrent";
                var context = new CommandContextImpl();

                // Should have a valid default value initially.
                Assert.Equal(10, context.GetBulkhead(groupKey).Available);

                // Negative limits aren't valid.
                ConfigProvider.Instance.Set(configKey, -1);
                Assert.Equal(10, context.GetBulkhead(groupKey).Available);

                // Zero (disallow all) is a valid value.
                ConfigProvider.Instance.Set(configKey, 0);
                Assert.Equal(0, context.GetBulkhead(groupKey).Available);
            }
        }
    }
}
