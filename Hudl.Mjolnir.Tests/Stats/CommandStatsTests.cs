using System;
using System.Threading.Tasks;
using Hudl.Config;
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
            var command = new ImmediatelyReturningCommandWithoutFallback
            {
                Stats = mockStats.Object,
            };

            await command.InvokeAsync();

            mockStats.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback InvokeAsync", "RanToCompletion", It.IsAny<TimeSpan>()), Times.Once);
            mockStats.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback ExecuteInIsolation", "RanToCompletion", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromReturnedTask()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskWithoutFallbackCommand
            {
                Stats = mockStats.Object
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback InvokeAsync", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback ExecuteInIsolation", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromExecute()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingExecuteWithoutFallbackCommand
            {
                Stats = mockStats.Object
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback InvokeAsync", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback ExecuteInIsolation", "Faulted", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_OperationCanceledException()
        {
            var mockStats = new Mock<IStats>();
            var command = new TimingOutWithoutFallbackCommand(TimeSpan.FromMilliseconds(100))
            {
                Stats = mockStats.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.GetBaseException() is OperationCanceledException);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback InvokeAsync", "Canceled", It.IsAny<TimeSpan>()), Times.Once);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback ExecuteInIsolation", "Canceled", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_RejectedException()
        {
            var mockStats = new Mock<IStats>();
            
            var mockBreaker = new Mock<ICircuitBreaker>();
            mockBreaker.Setup(m => m.IsAllowing()).Returns(false);

            // Will have been set by TestFixture constructor.
            Assert.True(new ConfigurableValue<bool>("mjolnir.useCircuitBreakers").Value);

            var command = new SuccessfulEchoCommandWithoutFallback("Test")
            {
                Stats = mockStats.Object,
                CircuitBreaker = mockBreaker.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.GetBaseException() is CircuitBreakerRejectedException);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback InvokeAsync", "Rejected", It.IsAny<TimeSpan>()), Times.Once);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback ExecuteInIsolation", "Rejected", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
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

            try
            {
                await command.InvokeAsync();
            }
            catch (ExpectedTestException e)
            {
                if (e != expected) throw;
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithEchoThrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackRethrowsInstigator()
        {
            var mockStats = new Mock<IStats>();
            var command = new FaultingTaskWithInstigatorRethrowingFallbackCommand
            {
                Stats = mockStats.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.IsFallbackImplemented);
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithInstigatorRethrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
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

            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithSuccessfulFallback TryFallback", "Success", It.IsAny<TimeSpan>()), Times.Once);
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

            mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithSuccessfulFallback TryFallback", "Success", It.IsAny<TimeSpan>()), Times.Once);
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

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                if (e.GetBaseException() != exception) throw;
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteEchoCommandWithoutFallback TryFallback", "NotImplemented", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();   
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

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                if (e.GetBaseException() != exception) throw;
                mockStats.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskEchoCommandWithoutFallback TryFallback", "NotImplemented", It.IsAny<TimeSpan>()), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
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

            mockStats.Verify(m => m.Elapsed(It.IsRegex(".*TryFallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
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

            mockStats.Verify(m => m.Elapsed(It.IsRegex(".*TryFallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }
    }
}
