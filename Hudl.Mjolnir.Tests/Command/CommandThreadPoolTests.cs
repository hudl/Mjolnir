using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Metrics;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Hudl.Mjolnir.ThreadPool;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandThreadPoolTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_ThreadPoolRejects_ThrowsCommandFailedExceptionWithRejectedStatusAndInnerException()
        {
            var exception = new IsolationThreadPoolRejectedException();
            var pool = new RejectingIsolationThreadPool(exception);
            // Had a tough time getting It.IsAny<Func<Task<object>>> to work with a mock pool, so I just stubbed one here.

            var command = new SuccessfulEchoCommandWithoutFallback(new {})
            {
                ThreadPool = pool,
            };

            var e = await Assert.ThrowsAsync<CommandRejectedException>(() => command.InvokeAsync());
            Assert.Equal(CommandCompletionStatus.Rejected, e.Status);
            Assert.Equal(exception, e.InnerException);
        }

        [Fact]
        public async Task InvokeAsync_ThreadPoolRejects_NotCountedByCircuitBreakerMetrics()
        {
            var exception = new IsolationThreadPoolRejectedException();
            var pool = new RejectingIsolationThreadPool(exception);

            var mockMetrics = new Mock<ICommandMetrics>();
            var mockBreaker = new Mock<ICircuitBreaker>();
            mockBreaker.Setup(m => m.IsAllowing()).Returns(true);
            mockBreaker.SetupGet(m => m.Metrics).Returns(mockMetrics.Object);

            var command = new SuccessfulEchoCommandWithoutFallback(new { })
            {
                CircuitBreaker = mockBreaker.Object,
                ThreadPool = pool,
            };

            var e = await Assert.ThrowsAsync<CommandRejectedException>(() => command.InvokeAsync());
            Assert.True(e.InnerException is IsolationThreadPoolRejectedException);
            mockMetrics.Verify(m => m.MarkCommandFailure(), Times.Never);
            mockMetrics.Verify(m => m.MarkCommandSuccess(), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_ThreadPoolRejects_InvokesFallback()
        {
            var exception = new IsolationThreadPoolRejectedException();
            var pool = new RejectingIsolationThreadPool(exception);

            var command = new SuccessfulEchoCommandWithFallback(new { })
            {
                ThreadPool = pool,
            };

            await command.InvokeAsync(); // Won't throw because there's a successful fallback.
            Assert.True(command.FallbackCalled);
        }

        private class RejectingIsolationThreadPool : IIsolationThreadPool
        {
            private readonly IsolationThreadPoolRejectedException _exceptionToThrow;

            public RejectingIsolationThreadPool(IsolationThreadPoolRejectedException exceptionToThrow)
            {
                _exceptionToThrow = exceptionToThrow;
            }

            public void Start()
            {
                throw new NotImplementedException();
            }

            public IWorkItem<TResult> Enqueue<TResult>(Func<TResult> func)
            {
                throw _exceptionToThrow;
            }

            public string Name { get { return "rejecting"; } }
        }
    }
}
