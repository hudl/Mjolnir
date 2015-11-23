using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class CommandStatsTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_Success()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var command = new ImmediatelyReturningCommandWithoutFallback
            {
                Stats = mockStats.Object,
                MetricEvents = mockMetricEvents.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback total", "RanToCompletion", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback execute", "RanToCompletion", It.IsAny<TimeSpan>()), Times.Once);
            mockMetricEvents.Verify(m => m.RejectedByBulkhead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromReturnedTask()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var command = new FaultingTaskWithoutFallbackCommand
            {
                Stats = mockStats.Object,
                MetricEvents = mockMetricEvents.Object,
            };

            await Assert.ThrowsAsync<CommandFailedException>(command.InvokeAsync);

            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback total", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback execute", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
            mockMetricEvents.Verify(m => m.RejectedByBulkhead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromExecute()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var command = new FaultingExecuteWithoutFallbackCommand
            {
                Stats = mockStats.Object,
                MetricEvents = mockMetricEvents.Object,
            };

            await Assert.ThrowsAsync<CommandFailedException>(command.InvokeAsync);
            
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback total", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback execute", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
            mockMetricEvents.Verify(m => m.RejectedByBulkhead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_OperationCanceledException()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var command = new TimingOutWithoutFallbackCommand(TimeSpan.FromMilliseconds(100))
            {
                Stats = mockStats.Object,
                MetricEvents = mockMetricEvents.Object,
            };

            var e = await Assert.ThrowsAsync<CommandTimeoutException>(command.InvokeAsync);

            Assert.True(e.GetBaseException() is OperationCanceledException);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback total", "TimedOut", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback execute", "TimedOut", It.IsAny<TimeSpan>()), Times.Once);
            mockMetricEvents.Verify(m => m.RejectedByBulkhead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_BreakerRejectedException()
        {
            var mockStats = new Mock<IStats>();
            var mockMetricEvents = new Mock<IMetricEvents>();
            var mockBreaker = new Mock<ICircuitBreaker>();
            mockBreaker.Setup(m => m.IsAllowing()).Returns(false);

            var command = new SuccessfulEchoCommandWithoutFallback("Test")
            {
                Stats = mockStats.Object,
                MetricEvents = mockMetricEvents.Object,
                CircuitBreaker = mockBreaker.Object,
            };

            var e = await Assert.ThrowsAsync<CommandRejectedException>(command.InvokeAsync);

            Assert.True(e.GetBaseException() is CircuitBreakerRejectedException);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback total", "Rejected", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback execute", "Rejected", It.IsAny<TimeSpan>()), Times.Once);

            // Since this was a breaker rejection, we don't expect a bulkhead rejection event here.
            mockMetricEvents.Verify(m => m.RejectedByBulkhead(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackThrowsNonInstigator()
        {
            var expected = new ExpectedTestException("foo");
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskWithEchoThrowingFallbackCommand(expected)
            {
                Stats = mockStats.Object,
            };

            var e = await Assert.ThrowsAsync<ExpectedTestException>(command.InvokeAsync);

            Assert.Equal(expected, e);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithEchoThrowingFallback fallback", "Failure", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackRethrowsInstigator()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskWithInstigatorRethrowingFallbackCommand
            {
                Stats = mockStats.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(command.InvokeAsync);

            Assert.True(e.IsFallbackImplemented);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithInstigatorRethrowingFallback fallback", "Failure", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackSucceeds()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskWithSuccessfulFallbackCommand
            {
                Stats = mockStats.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithSuccessfulFallback fallback", "Success", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndFallbackSucceeds()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingExecuteWithSuccessfulFallbackCommand
            {
                Stats = mockStats.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithSuccessfulFallback fallback", "Success", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndFallbackNotImplemented()
        {
            var exception = new ExpectedTestException("foo");
            var mockStats = new Mock<IStats>();
            var command = new FaultingExecuteEchoCommandWithoutFallback(exception)
            {
                Stats = mockStats.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(command.InvokeAsync);
            Assert.Equal(exception, e.GetBaseException());
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteEchoCommandWithoutFallback fallback", "NotImplemented", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackNotImplemented()
        {
            var exception = new ExpectedTestException("foo");
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskEchoCommandWithoutFallback(exception)
            {
                Stats = mockStats.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(command.InvokeAsync);

            Assert.Equal(exception, e.GetBaseException());
            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskEchoCommandWithoutFallback fallback", "NotImplemented", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_SuccessAndFallbackImplemented()
        {
            var mockStats = new Mock<IStats>();
            var command = new SuccessfulEchoCommandWithFallback("foo")
            {
                Stats = mockStats.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed(It.IsRegex(".*fallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_SuccessAndFallbackNotImplemented()
        {
            var mockStats = new Mock<IStats>();
            var command = new SuccessfulEchoCommandWithoutFallback("foo")
            {
                Stats = mockStats.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed(It.IsRegex(".*fallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }
    }
}
