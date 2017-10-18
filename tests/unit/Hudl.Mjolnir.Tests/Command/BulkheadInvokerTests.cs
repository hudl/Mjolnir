using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Config;
using static Hudl.Mjolnir.Bulkhead.BulkheadFactory;
using Hudl.Mjolnir.Metrics;

namespace Hudl.Mjolnir.Tests.Command
{
    public class BulkheadInvokerTests
    {
        public class ExecuteWithBulkheadAsync : TestFixture
        {
            [Fact]
            public async Task CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            {
                // The assumption tested here is important. If the bulkhead max concurrent
                // configuration value changes, the bulkhead holder will build a new semaphore and
                // swap it out with the old one. However, any bulkheads that acquired a lock on
                // the original semaphore need to release that semaphore instead of the new one.
                // Otherwise, they'll release on the new one. If that happens, the new semaphore's
                // counter won't be accurate respective to the number of commands concurrently
                // executing with it.

                // This test is complicated, but it's one of the most important unit tests in the
                // project. If you need to change it, take care that it gets re-written properly.

                // The test is performed by having the command itself change the config
                // value, which will happen between the TryEnter and Release calls.


                // Arrange

                const int initialMaxConcurrent = 10;
                const int newMaxConcurrent = 15;

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockConfig = new MjolnirConfiguration
                {
                    UseCircuitBreakers = true,
                    BulkheadConfigurations = new Dictionary<string, BulkheadConfiguration>
                    {
                        {
                            key,
                            new BulkheadConfiguration
                            {
                                MaxConcurrent = initialMaxConcurrent
                            }
                        }
                    }
                };

                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var mockCircuitBreaker = new Mock<ICircuitBreaker>(MockBehavior.Strict);
                mockCircuitBreaker.Setup(m => m.IsAllowing()).Returns(true);
                mockCircuitBreaker.Setup(m => m.Name).Returns(AnyString);
                mockCircuitBreaker.Setup(m => m.Metrics).Returns(new Mock<ICommandMetrics>().Object);
                mockCircuitBreaker.Setup(m => m.MarkSuccess(It.IsAny<long>()));

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(groupKey)).Returns(mockCircuitBreaker.Object);

                var mockMetricEvents = new Mock<IMetricEvents>(); // Non-Strict: we aren't testing metric events here, let's keep the test simpler.

                var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
                mockLogFactory.Setup(m => m.CreateLog<BulkheadFactory>()).Returns(new Mock<IMjolnirLog<BulkheadFactory>>().Object);
                mockLogFactory.Setup(m => m.CreateLog<SemaphoreBulkheadHolder>()).Returns(new Mock<IMjolnirLog<SemaphoreBulkheadHolder>>().Object);


                // Use a real BulkheadFactory, which will give us access to its BulkheadHolder.
                var bulkheadFactory = new BulkheadFactory(mockMetricEvents.Object, mockConfig, mockLogFactory.Object);
                var holder = bulkheadFactory.GetBulkheadHolder(groupKey);
                var initialBulkhead = bulkheadFactory.GetBulkhead(groupKey);

                // Use a real BreakerInvoker instead of a mocked one so that we actually
                // invoke the command that changes the config value.
                var breakerInvoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);
                var command = new ChangeBulkheadLimitAsyncCommand(key, holder, newMaxConcurrent);

                var invoker = new BulkheadInvoker(breakerInvoker, bulkheadFactory, mockMetricEvents.Object, mockConfig);
                var unusedCancellationToken = CancellationToken.None;

                // Make sure the BulkheadFactory has the expected Bulkhead initialized for the key.
                Assert.Equal(initialMaxConcurrent, bulkheadFactory.GetBulkhead(groupKey).CountAvailable);

                // Act

                var result = await invoker.ExecuteWithBulkheadAsync(command, unusedCancellationToken);

                // Assert

                // The assertions here are a bit indirect and, if we were mocking, could be more
                // deterministic. We check to see if the CountAvailable values change correctly.
                // Mocking would let us make Verify calls on TryEnter() and Release(), but mocking
                // is challenging because of how the BulkheadFactory internally keeps hold of the
                // Bulkheads it's managing within SemaphoreBulkheadHolders. The tests here should
                // be okay enough, though.


                // Since the config changed, the factory should have a new bulkhead for the key.
                var newBulkhead = bulkheadFactory.GetBulkhead(groupKey);
                Assert.True(initialBulkhead != newBulkhead);

                // The bulkhead we used should have its original value. We're making sure that
                // we didn't TryEnter() and then skip the Release() because a different bulkhead
                // was used.
                Assert.Equal(initialMaxConcurrent, initialBulkhead.CountAvailable);

                // For the sake of completeness, make sure the config change actually got
                // applied (otherwise we might not be testing an actual config change up
                // above).
                Assert.Equal(newMaxConcurrent, newBulkhead.CountAvailable);
            }

            [Fact]
            public async Task FiresMetricEventWhenRejected()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(false);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyAsyncCommand(key);

                // Act + Assert

                await Assert.ThrowsAsync<BulkheadRejectedException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBulkhead(key, command.Name));
            }

            [Fact]
            public async Task FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandSucceeds()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};
                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyAsyncCommand(key);

                // Act

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                // Assert

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public async Task FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandFails()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyThrowingAsyncCommand(key);

                // Act + Assert

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public async Task SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandSucceeds()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyAsyncCommand(key);

                // Act

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                // Assert

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public async Task SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandFails()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};

                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyThrowingAsyncCommand(key);

                // Act + Assert

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public async Task DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandSucceeds()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingAsyncCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreakerAsync(command, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(true));

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);

                // Act

                await invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None);

                // Assert

                Assert.Equal(0, command.ExecutionTimeMillis);
            }

            [Fact]
            public async Task DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandFails()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingAsyncCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreakerAsync(command, It.IsAny<CancellationToken>()))
                    .Throws(new ExpectedTestException(command.Name));

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};

                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);

                // Act + Assert

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBulkheadAsync(command, CancellationToken.None));

                Assert.Equal(0, command.ExecutionTimeMillis);
            }
        }

        public class ExecuteWithBulkhead : TestFixture
        {
            [Fact]
            public void CallsTryEnterAndReleaseOnTheSameBulkheadDuringConfigChange()
            {
                // The assumption tested here is important. If the bulkhead max concurrent
                // configuration value changes, the bulkhead holder will build a new semaphore and
                // swap it out with the old one. However, any bulkheads that acquired a lock on
                // the original semaphore need to release that semaphore instead of the new one.
                // Otherwise, they'll release on the new one. If that happens, the new semaphore's
                // counter won't be accurate respective to the number of commands concurrently
                // executing with it.

                // This test is complicated, but it's one of the most important unit tests in the
                // project. If you need to change it, take care that it gets re-written properly.

                // The test is performed by having the command itself change the config
                // value, which will happen between the TryEnter and Release calls.


                // Arrange

                const int initialMaxConcurrent = 10;
                const int newMaxConcurrent = 15;

                var key = AnyString;
                var groupKey = GroupKey.Named(key);


                var mockConfig = new MjolnirConfiguration
                {
                    UseCircuitBreakers = true,
                    BulkheadConfigurations = new Dictionary<string, BulkheadConfiguration>
                    {
                        {
                            key,
                            new BulkheadConfiguration
                            {
                                MaxConcurrent = initialMaxConcurrent
                            }
                        }
                    }
                };

                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var mockCircuitBreaker = new Mock<ICircuitBreaker>(MockBehavior.Strict);
                mockCircuitBreaker.Setup(m => m.IsAllowing()).Returns(true);
                mockCircuitBreaker.Setup(m => m.Name).Returns(AnyString);
                mockCircuitBreaker.Setup(m => m.Metrics).Returns(new Mock<ICommandMetrics>().Object);
                mockCircuitBreaker.Setup(m => m.MarkSuccess(It.IsAny<long>()));

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(groupKey)).Returns(mockCircuitBreaker.Object);

                var mockMetricEvents = new Mock<IMetricEvents>(); // Non-Strict: we aren't testing metric events here, let's keep the test simpler.

                var mockLogFactory = new Mock<IMjolnirLogFactory>(MockBehavior.Strict);
                mockLogFactory.Setup(m => m.CreateLog<BulkheadFactory>()).Returns(new Mock<IMjolnirLog<BulkheadFactory>>().Object);
                mockLogFactory.Setup(m => m.CreateLog<SemaphoreBulkheadHolder>()).Returns(new Mock<IMjolnirLog<SemaphoreBulkheadHolder>>().Object);
                // Use a real BulkheadFactory, which will give us access to its BulkheadHolder.
                var bulkheadFactory = new BulkheadFactory(mockMetricEvents.Object, mockConfig, mockLogFactory.Object);
                var holder = bulkheadFactory.GetBulkheadHolder(groupKey);
                var initialBulkhead = bulkheadFactory.GetBulkhead(groupKey);

                // Use a real BreakerInvoker instead of a mocked one so that we actually
                // invoke the command that changes the config value.
                var breakerInvoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);
                var command = new ChangeBulkheadLimitSyncCommand(key, holder, newMaxConcurrent);

                var invoker = new BulkheadInvoker(breakerInvoker, bulkheadFactory, mockMetricEvents.Object, mockConfig);
                var unusedCancellationToken = CancellationToken.None;

                // Make sure the BulkheadFactory has the expected Bulkhead initialized for the key.
                Assert.Equal(initialMaxConcurrent, bulkheadFactory.GetBulkhead(groupKey).CountAvailable);

                // Act

                var result = invoker.ExecuteWithBulkhead(command, unusedCancellationToken);

                // Assert

                // The assertions here are a bit indirect and, if we were mocking, could be more
                // deterministic. We check to see if the CountAvailable values change correctly.
                // Mocking would let us make Verify calls on TryEnter() and Release(), but mocking
                // is challenging because of how the BulkheadFactory internally keeps hold of the
                // Bulkheads it's managing within SemaphoreBulkheadHolders. The tests here should
                // be okay enough, though.


                // Since the config changed, the factory should have a new bulkhead for the key.
                var newBulkhead = bulkheadFactory.GetBulkhead(groupKey);
                Assert.True(initialBulkhead != newBulkhead);

                // The bulkhead we used should have its original value. We're making sure that
                // we didn't TryEnter() and then skip the Release() because a different bulkhead
                // was used.
                Assert.Equal(initialMaxConcurrent, initialBulkhead.CountAvailable);

                // For the sake of completeness, make sure the config change actually got
                // applied (otherwise we might not be testing an actual config change up
                // above).
                Assert.Equal(newMaxConcurrent, newBulkhead.CountAvailable);
            }

            [Fact]
            public void FiresMetricEventWhenRejected()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(false);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};
                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyCommand(key);

                // Act + Assert

                Assert.Throws<BulkheadRejectedException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBulkhead(key, command.Name));
            }

            [Fact]
            public void FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandSucceeds()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};

                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyCommand(key);

                // Act

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                // Assert

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public void FiresMetricEventWhenEnteringAndLeavingBulkheadAndCommandFails()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};
                // The breaker invoker behavior doesn't matter here, we shouldn't get to the point
                // where we try to use it. Pass a "false" value for useCircuitBreakers to help
                // ensure that.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyThrowingCommand(key);

                // Act + Assert

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.EnterBulkhead(key, command.Name));
                mockMetricEvents.Verify(m => m.LeaveBulkhead(key, command.Name));
            }

            [Fact]
            public void SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandSucceeds()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};
                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyCommand(key);

                // Act

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                // Assert

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public void SetsExecutionTimeOnCommandWhenInvokedWithoutBreakerAndCommandFails()
            {
                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = false};
                // Pass false for useCircuitBreakers to bypass the breaker; we're testing that here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);
                var command = new ConfigurableKeyThrowingCommand(key);

                // Act + Assert

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                Assert.True(command.ExecutionTimeMillis > 0);
            }

            [Fact]
            public void DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandSucceeds()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreaker(command, It.IsAny<CancellationToken>()))
                    .Returns(true);

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};
                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);

                // Act

                invoker.ExecuteWithBulkhead(command, CancellationToken.None);

                // Assert

                Assert.Equal(0, command.ExecutionTimeMillis);
            }

            [Fact]
            public void DoesntSetExecutionTimeOnCommandWhenInvokedWithBreakerAndCommandFails()
            {
                // If we execute on the breaker, the breaker should set the execution time instead
                // of the bulkhead invoker.

                // Arrange

                var key = AnyString;
                var groupKey = GroupKey.Named(key);

                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockBreakerInvoker = new Mock<IBreakerInvoker>();

                var mockBulkhead = new Mock<ISemaphoreBulkhead>();
                mockBulkhead.Setup(m => m.TryEnter()).Returns(true);
                mockBulkhead.SetupGet(m => m.Name).Returns(key);

                var mockBulkheadFactory = new Mock<IBulkheadFactory>(MockBehavior.Strict);
                mockBulkheadFactory.Setup(m => m.GetBulkhead(groupKey)).Returns(mockBulkhead.Object);

                var command = new ConfigurableKeyThrowingCommand(key);
                mockBreakerInvoker.Setup(m => m.ExecuteWithBreaker(command, It.IsAny<CancellationToken>()))
                    .Throws(new ExpectedTestException(command.Name));

                var mockConfig = new MjolnirConfiguration {UseCircuitBreakers = true};
                // Pass true for useCircuitBreakers, we need to test that behavior here.
                var invoker = new BulkheadInvoker(mockBreakerInvoker.Object, mockBulkheadFactory.Object, mockMetricEvents.Object, mockConfig);

                // Act + Assert

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBulkhead(command, CancellationToken.None));

                Assert.Equal(0, command.ExecutionTimeMillis);
            }
        }

        // Changes the maxConcurrent for a bulkhead during command execution, which is a fakey
        // but useful way to test bulkhead behavior (see the test that uses this).
        internal class ChangeBulkheadLimitAsyncCommand : AsyncCommand<bool>
        {
            private readonly SemaphoreBulkheadHolder _holder;
            private readonly int _changeLimitTo;

            public ChangeBulkheadLimitAsyncCommand(string bulkheadKey, SemaphoreBulkheadHolder holder, int changeLimitTo)
                : base(bulkheadKey, bulkheadKey, TimeSpan.FromSeconds(1000))
            {
                _holder = holder;
                _changeLimitTo = changeLimitTo;
            }

            public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
            {
                _holder.UpdateMaxConcurrent(_changeLimitTo);
                return Task.FromResult(true);
            }
        }

        // Changes the maxConcurrent for a bulkhead during command execution, which is a fakey
        // but useful way to test bulkhead behavior (see the test that uses this).
        internal class ChangeBulkheadLimitSyncCommand : SyncCommand<bool>
        {
            private readonly SemaphoreBulkheadHolder _holder;
            private readonly int _changeLimitTo;

            public ChangeBulkheadLimitSyncCommand(string bulkheadKey, SemaphoreBulkheadHolder holder, int changeLimitTo)
                : base(bulkheadKey, bulkheadKey, TimeSpan.FromSeconds(1000))
            {
                _holder = holder;
                _changeLimitTo = changeLimitTo;
            }

            public override bool Execute(CancellationToken cancellationToken)
            {
                _holder.UpdateMaxConcurrent(_changeLimitTo);
                return true;
            }
        }

        // Allows a configurable isolation key. Command execution succeeds.
        internal class ConfigurableKeyAsyncCommand : AsyncCommand<bool>
        {
            public ConfigurableKeyAsyncCommand(string key) : base(key, key, TimeSpan.FromSeconds(1000))
            { }

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
