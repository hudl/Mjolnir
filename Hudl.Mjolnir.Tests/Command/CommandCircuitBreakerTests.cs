using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandCircuitBreakerTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_WhenBreakerNotAllowing_ThrowsException()
        {
            var mockBreaker = CreateMockBreaker(false);
            var command = new SuccessfulEchoCommandWithoutFallback(null)
            {
                CircuitBreaker = mockBreaker.Object,
            };

            var e = await Assert.ThrowsAsync<CommandRejectedException>(() => command.InvokeAsync());
            Assert.True(e.InnerException is CircuitBreakerRejectedException);
        }

        [Fact]
        public async Task InvokeAsync_WhenCommandSuccessful_MarksBreakerSuccess()
        {
            var mockBreaker = CreateMockBreaker(true);
            var command = new SuccessfulEchoCommandWithoutFallback(null)
            {
                CircuitBreaker = mockBreaker.Object,
            };
            
            await command.InvokeAsync();

            mockBreaker.Verify(m => m.MarkSuccess(It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenCommandSuccessful_MarksMetricsCommandSuccess()
        {
            var mockMetrics = new Mock<ICommandMetrics>();
            var mockBreaker = CreateMockBreaker(true, mockMetrics);
            var command = new SuccessfulEchoCommandWithoutFallback(null)
            {
                CircuitBreaker = mockBreaker.Object,
            };

            await command.InvokeAsync();

            mockMetrics.Verify(m => m.MarkCommandSuccess(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenExecuteThrowsException_RethrowsException()
        {
            var exception = new ExpectedTestException("Expected");

            var mockBreaker = CreateMockBreaker(true);
            var command = new FaultingExecuteEchoCommandWithoutFallback(exception)
            {
                CircuitBreaker = mockBreaker.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            Assert.True(e.InnerException == exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenReturnedTaskThrowsException_RethrowsException()
        {
            var exception = new ExpectedTestException("Expected");

            var mockBreaker = CreateMockBreaker(true);
            var command = new FaultingTaskEchoCommandWithoutFallback(exception)
            {
                CircuitBreaker = mockBreaker.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            Assert.True(e.InnerException == exception);
        }

        [Fact]
        public async Task InvokeAsync_WhenCommandThrowsException_MarksMetricsCommandFailure()
        {
            var mockMetrics = new Mock<ICommandMetrics>();
            var mockBreaker = CreateMockBreaker(true, mockMetrics);
            
            var command = new FaultingTaskWithoutFallbackCommand
            {
                CircuitBreaker = mockBreaker.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            mockMetrics.Verify(m => m.MarkCommandFailure(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenSuccessful_ReturnsCommandResult()
        {
            var expected = DateTime.UtcNow.Ticks;

            var mockBreaker = CreateMockBreaker(true);
            var command = new SuccessfulEchoCommandWithoutFallback(expected)
            {
                CircuitBreaker = mockBreaker.Object,
            };
            
            var result = await command.InvokeAsync();

            Assert.Equal(expected, result);
        }

        private static Mock<ICircuitBreaker> CreateMockBreaker(bool isAllowing, IMock<ICommandMetrics> mockMetrics = null)
        {
            var breaker = new Mock<ICircuitBreaker>();
            breaker.Setup(m => m.IsAllowing()).Returns(isAllowing);

            if (mockMetrics != null)
            {
                breaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);
            }
            else
            {
                var metrics = new Mock<ICommandMetrics>();
                //metrics.Setup(m => m.GetSnapshot()).Returns(new MetricsSnapshot(100, 10));
                breaker.SetupGet(m => m.Metrics).Returns(metrics.Object);
            }

            return breaker;
        }
    }
}
