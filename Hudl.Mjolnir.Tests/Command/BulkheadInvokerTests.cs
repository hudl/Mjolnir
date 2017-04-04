using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class BulkheadInvokerTests
    {
        public class ExecuteWithBulkheadAsync : TestFixture
        {
            // TODO rewrite this test, it's different now that config is mock-able
            //[Fact]
            //public void CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            //{
            //    // A little more widely-scoped than a unit - we're testing the interaction
            //    // between the invoker and command context here.

            //    // The test is performed by having the command itself change the config
            //    // value, which will happen between the TryEnter and Release calls.

            //    var key = Rand.String();
            //    var groupKey = GroupKey.Named(key);
            //    var bulkheadConfig = "mjolnir.bulkhead." + key + ".maxConcurrent";

            //    var context = new CommandContextImpl();

            //    var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            //    mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

            //    var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
            //    mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

            //    // Use a real breaker invoker instead of a mocked one so that we actually
            //    // invoke the command (to change the config).
            //    var breakerInvoker = new BreakerInvoker(context, mockBreakerExceptionHandler.Object);
            //    var command = new ChangeBulkheadLimitAsyncCommand(key, bulkheadConfig, 15); // Limit needs to be different from default.

            //    // This is the bulkhead that should be used for both TryEnter and Release.
            //    // We don't want to mock it because we're also testing the behavior of
            //    // the CommandContext internals here when a config changes. Mocking would
            //    // allow for a more accurate test, though, since we'd be able to actually
            //    // check if Release and TryEnter were both called on the same object.
            //    var bulkhead = context.GetBulkhead(groupKey);
            //    var invoker = new BulkheadInvoker(breakerInvoker, context, mockConfig.Object);
            //    var unusedCancellationToken = CancellationToken.None;

            //    // Make sure we know the bulkhead value before the test.
            //    Assert.Equal(10, bulkhead.CountAvailable);

            //    // The test - should cause the bulkhead to be used during a config change.
            //    var result = invoker.ExecuteWithBulkheadAsync(command, unusedCancellationToken);

            //    // The bulkhead we used should have its original value. We're making sure that
            //    // we didn't TryEnter() and then skip the release because a different bulkhead
            //    // was used.
            //    Assert.Equal(10, bulkhead.CountAvailable);

            //    // For the sake of completeness, make sure the config change actually got
            //    // applied (otherwise we might not be testing an actual config change up
            //    // above.
            //    Assert.Equal(15, context.GetBulkhead(groupKey).CountAvailable);
            //}

            [Fact]
            public async Task FiresMetricEventWhenRejected()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(false);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyAsyncCommand(key);

                await Assert.ThrowsAsync<BulkheadRejectedException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBulkhead(key, command.Name));
            }

            [Fact]
            public async Task FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandSucceeds()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyAsyncCommand(key);

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public async Task FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandFails()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyThrowingAsyncCommand(key);

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public async Task SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandSucceeds()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyAsyncCommand(key);

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public async Task SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandFails()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyThrowingAsyncCommand(key);

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public async Task DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandSucceeds()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingAsyncCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreakerAsync(command, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(true));

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                Assert.Equal(0, command.ExecutionTimeMillis);
            }

            [Fact]
            public async Task DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandFails()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingAsyncCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreakerAsync(command, It.IsAny<CancellationToken>()))
                    .Throws(new ExpectedTestException(command.Name));

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                
                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                Assert.Equal(0, command.ExecutionTimeMillis);
            }
        }

        public class ExecuteWithBulkhead : TestFixture
        {
            // TODO rewrite this test, it's different now that config is mock-able
            //[Fact]
            //public void CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            //{
            //    // A little more widely-scoped than a unit - we're testing the interaction
            //    // between the invoker and command context here.

            //    // The test is performed by having the command itself change the config
            //    // value, which will happen between the TryEnter and Release calls.

            //    var key = Rand.String();
            //    var groupKey = GroupKey.Named(key);
            //    var bulkheadConfig = "mjolnir.bulkhead." + key + ".maxConcurrent";
                
            //    var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            //    mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

            //    var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
            //    mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

            //    // Use a real breaker invoker instead of a mocked one so that we actually
            //    // invoke the command (to change the config).
            //    var breakerInvoker = new BreakerInvoker(context, mockBreakerExceptionHandler.Object);
            //    var command = new ChangeBulkheadLimitSyncCommand(key, bulkheadConfig, 15); // Limit needs to be different from default.

            //    // This is the bulkhead that should be used for both TryEnter and Release.
            //    // We don't want to mock it because we're also testing the behavior of
            //    // the CommandContext internals here when a config changes. Mocking would
            //    // allow for a more accurate test, though, since we'd be able to actually
            //    // check if Release and TryEnter were both called on the same object.
            //    var bulkhead = context.GetBulkhead(groupKey);
            //    var invoker = new BulkheadInvoker(breakerInvoker, context, mockConfig.Object);
            //    var unusedCancellationToken = CancellationToken.None;

            //    // Make sure we know the bulkhead value before the test.
            //    Assert.Equal(10, bulkhead.CountAvailable);

            //    // The test - should cause the bulkhead to be used during a config change.
            //    var result = invoker.ExecuteWithBulkhead(command, unusedCancellationToken);

            //    // The bulkhead we used should have its original value. We're making sure that
            //    // we didn't TryEnter() and then skip the release because a different bulkhead
            //    // was used.
            //    Assert.Equal(10, bulkhead.CountAvailable);

            //    // For the sake of completeness, make sure the config change actually got
            //    // applied (otherwise we might not be testing an actual config change up
            //    // above.
            //    Assert.Equal(15, context.GetBulkhead(groupKey).CountAvailable);
            //}

            [Fact]
            public void FiresMetricEventWhenRejected()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(false);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyCommand(key);

                Assert.Throws<BulkheadRejectedException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBulkhead(key, command.Name));
            }

            [Fact]
            public void FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandSucceeds()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyCommand(key);

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public void FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandFails()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyThrowingCommand(key);

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public void SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandSucceeds()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyCommand(key);

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public void SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandFails()
            {
                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(false);

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);
                var command = new ConfigurableKeyThrowingCommand(key);

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public void DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandSucceeds()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreaker(command, It.IsAny<CancellationToken>()))
                    .Returns(true);

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                Assert.Equal(0, command.ExecutionTimeMillis);
            }

            [Fact]
            public void DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandFails()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                var key = Rand.String();
                var groupKey = GroupKey.Named(key);
                
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBulkhead = new Mock<IBulkheadSemaphore>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();
                
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);
                
                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreaker(command, It.IsAny<CancellationToken>()))
                    .Throws(new ExpectedTestException(command.Name));

                var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
                mockConfig.Setup(m => m.GetConfig("mjolnir.useCircuitBreakers", It.IsAny<bool>())).Returns(true);

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig.Object);

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                Assert.Equal(0, command.ExecutionTimeMillis);
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
                // TODO rewrite the test that uses this class
                //ConfigProvider.Instance.Set(_configKey, _changeLimitTo);
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
                // TODO rewrite the test that uses this class
                //ConfigProvider.Instance.Set(_configKey, _changeLimitTo);
                return true;
            }
        }

        // Allows a configurable isolation key. Command execution succeeds.
        internal class ConfigurableKeyAsyncCommand : AsyncCommand<bool>
        {
            public ConfigurableKeyAsyncCommand(string key) : base(key, key, TimeSpan.FromSeconds(1000))
            {}

            public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }

        // Allows a configurable isolation key. Command execution fails.
        internal class ConfigurableKeyThrowingAsyncCommand : AsyncCommand<bool>
        {
            public ConfigurableKeyThrowingAsyncCommand(string key)
                : base(key, key, TimeSpan.FromSeconds(1000))
            { }

            public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new ExpectedTestException(Name);
            }
        }

        // Allows a configurable isolation key. Command execution succeeds.
        internal class ConfigurableKeyCommand : SyncCommand<bool>
        {
            public ConfigurableKeyCommand(string key)
                : base(key, key, TimeSpan.FromSeconds(1000))
            { }

            public override bool Execute(CancellationToken cancellationToken)
            {
                return true;
            }
        }

        // Allows a configurable isolation key. Command execution fails.
        internal class ConfigurableKeyThrowingCommand : SyncCommand<bool>
        {
            public ConfigurableKeyThrowingCommand(string key)
                : base(key, key, TimeSpan.FromSeconds(1000))
            { }

            public override bool Execute(CancellationToken cancellationToken)
            {
                throw new ExpectedTestException(Name);
            }
        }
    }
}
