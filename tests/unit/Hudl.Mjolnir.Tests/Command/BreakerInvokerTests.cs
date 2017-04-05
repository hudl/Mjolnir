using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class BreakerInvokerTests
    {
        public class ExecuteWithBreakerAsync : TestFixture
        {
            [Fact]
            public async Task WhenRejected_FiresBreakerRejectedMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(false); // We want to reject.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);

                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpAsyncCommand();
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                await Assert.ThrowsAsync<CircuitBreakerRejectedException>(() => invoker.ExecuteWithBreakerAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBreaker(key, command.Name));
            }
            
            [Fact]
            public async Task WhenSuccessful_FiresBreakerSuccessCountMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(true); // We want to get past the initial rejection check.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);
                
                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpFailingAsyncCommand();
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                await Assert.ThrowsAsync<ExpectedTestException>(() => invoker.ExecuteWithBreakerAsync(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.BreakerFailureCount(key, command.Name));
            }

            [Fact]
            public async Task WhenFailed_FiresBreakerFailureCountMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(true); // We want to get past the initial rejection check.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);
                
                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpAsyncCommand(); // Should be successful.
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                await invoker.ExecuteWithBreakerAsync(command, CancellationToken.None);

                mockMetricEvents.Verify(m => m.BreakerSuccessCount(key, command.Name));
            }
        }

        public class ExecuteWithBreaker : TestFixture
        {
            [Fact]
            public void WhenRejected_FiresBreakerRejectedMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(false); // We want to reject.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);
                
                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpCommand();
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                Assert.Throws<CircuitBreakerRejectedException>(() => invoker.ExecuteWithBreaker(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.RejectedByBreaker(key, command.Name));
            }

            [Fact]
            public void WhenSuccessful_FiresBreakerSuccessCountMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(true); // We want to get past the initial rejection check.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);
                
                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpFailingCommand();
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                Assert.Throws<ExpectedTestException>(() => invoker.ExecuteWithBreaker(command, CancellationToken.None));

                mockMetricEvents.Verify(m => m.BreakerFailureCount(key, command.Name));
            }

            [Fact]
            public void WhenFailed_FiresBreakerFailureCountMetricEvent()
            {
                var key = Rand.String();
                
                var mockBreaker = new Mock<ICircuitBreaker>();
                var mockMetricEvents = new Mock<IMetricEvents>();
                var mockMetrics = new Mock<ICommandMetrics>();
                mockBreaker.SetupGet(m => m.Name).Returns(key);
                mockBreaker.Setup(m => m.IsAllowing()).Returns(true); // We want to get past the initial rejection check.
                mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

                var mockCircuitBreakerFactory = new Mock<ICircuitBreakerFactory>(MockBehavior.Strict);
                mockCircuitBreakerFactory.Setup(m => m.GetCircuitBreaker(It.IsAny<GroupKey>())).Returns(mockBreaker.Object);
                
                var mockBreakerExceptionHandler = new Mock<IBreakerExceptionHandler>(MockBehavior.Strict);
                mockBreakerExceptionHandler.Setup(m => m.IsExceptionIgnored(It.IsAny<Type>())).Returns(false);

                var command = new NoOpCommand(); // Should be successful.
                var invoker = new BreakerInvoker(mockCircuitBreakerFactory.Object, mockMetricEvents.Object, mockBreakerExceptionHandler.Object);

                invoker.ExecuteWithBreaker(command, CancellationToken.None);

                mockMetricEvents.Verify(m => m.BreakerSuccessCount(key, command.Name));
            }
        }

        internal class NoOpFailingAsyncCommand : AsyncCommand<bool>
        {
            public NoOpFailingAsyncCommand() : base("test", "test", TimeSpan.FromSeconds(1000)) { }

            public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new ExpectedTestException("Expected");
            }
        }

        internal class NoOpFailingCommand : SyncCommand<bool>
        {
            public NoOpFailingCommand() : base("test", "test", TimeSpan.FromSeconds(1000)) { }

            public override bool Execute(CancellationToken cancellationToken)
            {
                throw new ExpectedTestException("Expected");
            }
        }
    }
}
