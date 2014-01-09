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
    public class CommandFallbackTests
    {
        private readonly Random _random = new Random();

        [Fact]
        public async Task InvokeAsync_TimesOutAndNoFallback_ThrowsCommandException()
        {
            var command = new TimingOutWithoutFallbackCommand();
            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TimesOutAndFallbackThrows_RethrowsFallbackException()
        {
            var expected = new ExpectedTestException("Expected rethrown exception");
            var command = new TimingOutWithEchoThrowingFallbackCommand(expected);
            try
            {
                await command.InvokeAsync();
            }
            catch (ExpectedTestException e)
            {
                Assert.Equal(expected, e);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TimesOutAndFallbackSucceeds_ReturnsFallbackResult()
        {
            var command = new TimingOutWithSuccessfulFallbackCommand();
            var result = await command.InvokeAsync();
            Assert.NotNull(result);
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndNoFallback_ThrowsCommandException()
        {
            var command = new FaultingWithoutFallbackCommand();
            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackThrows_RethrowsFallbackException()
        {
            var expected = new ExpectedTestException("Expected rethrown exception");
            var command = new FaultingWithEchoThrowingFallbackCommand(expected);
            try
            {
                await command.InvokeAsync();
            }
            catch (ExpectedTestException e)
            {
                Assert.Equal(expected, e);
                return;
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackSucceeds_ReturnsFallbackResult()
        {
            var command = new FaultingWithSuccessfulFallbackCommand();
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
        public async Task InvokeAsync_CommandException_RetainsOriginalExceptionCause()
        {
            var cause = new ExpectedTestException("Root cause exception");
            var command = new EchoThrowingCommandWithoutFallback(cause);

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.Equal(cause, e.GetBaseException());
                return;
            }

            AssertX.FailExpectedException();
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

            var command = new FaultingWithSuccessfulFallbackCommand
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

            var command = new FaultingWithSuccessfulFallbackCommand
            {
                FallbackSemaphore = mockSemaphore.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                mockSemaphore.Verify(m => m.TryEnter(), Times.Once);
                Assert.Equal(FallbackStatus.Rejected, e.FallbackStatus);
                return;
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_WhenFallbackFails_ReleasesSemaphore()
        {
            var mockSemaphore = new Mock<IIsolationSemaphore>();
            mockSemaphore.Setup(m => m.TryEnter()).Returns(true);

            var exception = new ExpectedTestException("Expected");
            var command = new FaultingWithEchoThrowingFallbackCommand(exception)
            {
                FallbackSemaphore = mockSemaphore.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (ExpectedTestException e)
            {
                Assert.Equal(exception, e);
                mockSemaphore.Verify(m => m.Release(), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        private class TimingOutWithSuccessfulFallbackCommand : TimingOutWithoutFallbackCommand
        {
            protected override object Fallback(CommandFailedException instigator)
            {
                return new { };
            }
        }

        private class TimingOutWithEchoThrowingFallbackCommand : TimingOutWithoutFallbackCommand
        {
            private readonly ExpectedTestException _exception;

            internal TimingOutWithEchoThrowingFallbackCommand(ExpectedTestException toRethrow)
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
