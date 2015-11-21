using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Hudl.Mjolnir.ThreadPool;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandFallbackTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_TimesOutAndNoFallback_ThrowsCommandException()
        {
            var command = new TimingOutWithoutFallbackCommand(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAsync<CommandTimeoutException>(() => command.InvokeAsync());
        }

        [Fact]
        public async Task InvokeAsync_TimesOutAndFallbackThrows_RethrowsFallbackException()
        {
            var expected = new ExpectedTestException("Expected rethrown exception");
            var command = new TimingOutWithEchoThrowingFallbackCommand(expected);

            var e = await Assert.ThrowsAsync<ExpectedTestException>(() => command.InvokeAsync());
            Assert.Equal(expected, e);
        }

        [Fact]
        public async Task InvokeAsync_TimesOutAndFallbackSucceeds_ReturnsFallbackResult()
        {
            var command = new TimingOutWithSuccessfulFallbackCommand();
            var result = await command.InvokeAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndNoFallback_ThrowsCommandException()
        {
            var command = new FaultingTaskWithoutFallbackCommand();
            await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndNoFallback_ThrowsCommandException()
        {
            var command = new FaultingExecuteWithoutFallbackCommand();
            await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackThrows_RethrowsFallbackException()
        {
            var expected = new ExpectedTestException("Expected rethrown exception");
            var command = new FaultingTaskWithEchoThrowingFallbackCommand(expected);

            var e = await Assert.ThrowsAsync<ExpectedTestException>(() => command.InvokeAsync());
            Assert.Equal(expected, e);
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackSucceeds_ReturnsFallbackResult()
        {
            var command = new FaultingTaskWithSuccessfulFallbackCommand();
            var result = await command.InvokeAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndFallbackSucceeds_ReturnsFallbackResult()
        {
            var command = new FaultingExecuteWithSuccessfulFallbackCommand();
            var result = await command.InvokeAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InvokeAsync_Succeeds_DoesntInvokeFallback()
        {
            const string value = "value";
            var command = new SuccessfulEchoCommandWithFallback(value);
            var result = await command.InvokeAsync();
            Assert.Equal(value, result);
            Assert.False(command.FallbackCalled);
        }

        [Fact]
        public async Task InvokeAsync_CommandExceptionFromExecute_RetainsOriginalExceptionCause()
        {
            var cause = new ExpectedTestException("Root cause exception");
            var command = new FaultingExecuteEchoCommandWithoutFallback(cause);

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            Assert.Equal(cause, e.GetBaseException());
        }

        [Fact]
        public async Task InvokeAsync_CommandExceptionFromTask_RetainsOriginalExceptionCause()
        {
            var cause = new ExpectedTestException("Root cause exception");
            var command = new FaultingTaskEchoCommandWithoutFallback(cause);

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            Assert.Equal(cause, e.GetBaseException());
        }

        [Fact]
        public async Task InvokeAsync_WhenBreakerRejectedButFallbackDefined_InvokesFallback()
        {
            var expected = new { };

            var mockBreaker = new Mock<ICircuitBreaker>();
            mockBreaker.Setup(m => m.IsAllowing()).Returns(false);

            var command = new SuccessfulEchoCommandWithFallback(expected)
            {
                CircuitBreaker = mockBreaker.Object,
            };

            var result = await command.InvokeAsync();
            Assert.True(command.FallbackCalled);
            Assert.Equal(expected, result);
        }
        
        [Fact]
        public async Task InvokeAsync_WhenFallbackSemaphoreAvailable_InvokesFallbackAndReleasesSemaphore()
        {
            var mockSemaphore = new Mock<IIsolationSemaphore>();
            mockSemaphore.Setup(m => m.TryEnter()).Returns(true);

            var command = new FaultingTaskWithSuccessfulFallbackCommand
            {
                FallbackSemaphore = mockSemaphore.Object,
            };

            await command.InvokeAsync(); // Shouldn't throw instigator exception.
            
            mockSemaphore.Verify(m => m.Release(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WhenFallbackSemaphoreNotAvailable_RethrowsInstigatorImmediatelyWithRejectedFallbackStatus()
        {
            var mockSemaphore = new Mock<IIsolationSemaphore>();
            mockSemaphore.Setup(m => m.TryEnter()).Returns(false);

            var command = new FaultingTaskWithSuccessfulFallbackCommand
            {
                FallbackSemaphore = mockSemaphore.Object,
            };

            var e = await Assert.ThrowsAsync<CommandFailedException>(() => command.InvokeAsync());
            mockSemaphore.Verify(m => m.TryEnter(), Times.Once);
            Assert.Equal(FallbackStatus.Rejected, e.FallbackStatus);
        }

        [Fact]
        public async Task InvokeAsync_WhenFallbackFails_ReleasesSemaphore()
        {
            var mockSemaphore = new Mock<IIsolationSemaphore>();
            mockSemaphore.Setup(m => m.TryEnter()).Returns(true);

            var exception = new ExpectedTestException("Expected");
            var command = new FaultingTaskWithEchoThrowingFallbackCommand(exception)
            {
                FallbackSemaphore = mockSemaphore.Object,
            };

            var e = await Assert.ThrowsAsync<ExpectedTestException>(() => command.InvokeAsync());
            Assert.Equal(exception, e);
            mockSemaphore.Verify(m => m.Release(), Times.Once);
        }

        private class TimingOutWithSuccessfulFallbackCommand : TimingOutWithoutFallbackCommand
        {
            public TimingOutWithSuccessfulFallbackCommand() : base(TimeSpan.FromMilliseconds(100)) {}

            protected override object Fallback(CommandFailedException instigator)
            {
                return new { };
            }
        }

        private class TimingOutWithEchoThrowingFallbackCommand : TimingOutWithoutFallbackCommand
        {
            private readonly ExpectedTestException _exception;

            internal TimingOutWithEchoThrowingFallbackCommand(ExpectedTestException toRethrow) : base(TimeSpan.FromMilliseconds(100))
            {
                _exception = toRethrow;
            }

            protected override object Fallback(CommandFailedException instigator)
            {
                throw _exception;
            }
        }
    }
}
