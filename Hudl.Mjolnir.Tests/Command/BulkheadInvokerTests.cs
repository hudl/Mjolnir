using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class BulkheadInvokerTests
    {
        public class ExecuteWithBulkheadAsync : TestFixture
        {
            [Fact]
            public void CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            {
                // A little more widely-scoped than a unit - we're testing the interaction
                // between the invoker and command context here.

                // The test is performed by having the command itself change the config
                // value, which will happen between the TryEnter and Release calls.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                var bulkheadConfig = "mjolnir.bulkheads." + key + ".maxConcurrent";

                var context = new CommandContextImpl();

                // Use a real breaker invoker instead of a mocked one so that we actually
                // invoke the command (to change the config).
                var breakerInvoker = new BreakerInvoker(context);
                var command = new ChangeBulkheadLimitAsyncCommand(key, bulkheadConfig, 15); // Limit needs to be different from default.

                // This is the bulkhead that should be used for both TryEnter and Release.
                // We don't want to mock it because we're also testing the behavior of
                // the CommandContext internals here when a config changes. Mocking would
                // allow for a more accurate test, though, since we'd be able to actually
                // check if Release and TryEnter were both called on the same object.
                var bulkhead = context.GetBulkhead(groupKey);
                var invoker = new BulkheadInvoker(breakerInvoker, context);
                var unusedCancellationToken = CancellationToken.None;

                // Make sure we know the bulkhead value before the test.
                Assert.Equal(10, bulkhead.Available);

                // The test - should cause the bulkhead to be used during a config change.
                var result = invoker.ExecuteWithBulkheadAsync(command, unusedCancellationToken);

                // The bulkhead we used should have its original value. We're making sure that
                // we didn't TryEnter() and then skip the release because a different bulkhead
                // was used.
                Assert.Equal(10, bulkhead.Available);

                // For the sake of completeness, make sure the config change actually got
                // applied (otherwise we might not be testing an actual config change up
                // above.
                Assert.Equal(15, context.GetBulkhead(groupKey).Available);
            }
        }

        public class ExecuteWithBulkhead : TestFixture
        {
            [Fact]
            public void CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            {
                // A little more widely-scoped than a unit - we're testing the interaction
                // between the invoker and command context here.

                // The test is performed by having the command itself change the config
                // value, which will happen between the TryEnter and Release calls.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                var bulkheadConfig = "mjolnir.bulkheads." + key + ".maxConcurrent";

                var context = new CommandContextImpl();

                // Use a real breaker invoker instead of a mocked one so that we actually
                // invoke the command (to change the config).
                var breakerInvoker = new BreakerInvoker(context);
                var command = new ChangeBulkheadLimitSyncCommand(key, bulkheadConfig, 15); // Limit needs to be different from default.

                // This is the bulkhead that should be used for both TryEnter and Release.
                // We don't want to mock it because we're also testing the behavior of
                // the CommandContext internals here when a config changes. Mocking would
                // allow for a more accurate test, though, since we'd be able to actually
                // check if Release and TryEnter were both called on the same object.
                var bulkhead = context.GetBulkhead(groupKey);
                var invoker = new BulkheadInvoker(breakerInvoker, context);
                var unusedCancellationToken = CancellationToken.None;

                // Make sure we know the bulkhead value before the test.
                Assert.Equal(10, bulkhead.Available);

                // The test - should cause the bulkhead to be used during a config change.
                var result = invoker.ExecuteWithBulkhead(command, unusedCancellationToken);

                // The bulkhead we used should have its original value. We're making sure that
                // we didn't TryEnter() and then skip the release because a different bulkhead
                // was used.
                Assert.Equal(10, bulkhead.Available);

                // For the sake of completeness, make sure the config change actually got
                // applied (otherwise we might not be testing an actual config change up
                // above.
                Assert.Equal(15, context.GetBulkhead(groupKey).Available);
            }
        }

        // Has a configurable key.
        internal class ChangeBulkheadLimitAsyncCommand : AsyncCommand<bool>
        {
            private readonly string _configKey;
            private readonly int _changeLimitTo;

            public ChangeBulkheadLimitAsyncCommand(string bulkheadKey, string configKey, int changeLimitTo)
                : base(bulkheadKey, bulkheadKey, TimeSpan.FromSeconds(1000))
            {
                _configKey = configKey;
                _changeLimitTo = changeLimitTo;
            }

            public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
            {
                ConfigProvider.Instance.Set(_configKey, _changeLimitTo);
                return Task.FromResult(true);
            }
        }

        // Has a configurable key.
        internal class ChangeBulkheadLimitSyncCommand : SyncCommand<bool>
        {
            private readonly string _configKey;
            private readonly int _changeLimitTo;

            public ChangeBulkheadLimitSyncCommand(string bulkheadKey, string configKey, int changeLimitTo)
                : base(bulkheadKey, bulkheadKey, TimeSpan.FromSeconds(1000))
            {
                _configKey = configKey;
                _changeLimitTo = changeLimitTo;
            }

            public override bool Execute(CancellationToken cancellationToken)
            {
                ConfigProvider.Instance.Set(_configKey, _changeLimitTo);
                return true;
            }
        }
    }
}
