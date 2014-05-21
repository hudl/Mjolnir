using System;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Hudl.Riemann;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Stats
{
    public class CommandStatsTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_Success()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new ImmediatelyReturningCommandWithoutFallback
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback InvokeAsync", "RanToCompletion", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Elapsed("mjolnir command test.ImmediatelyReturningCommandWithoutFallback ExecuteInIsolation", "RanToCompletion", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromReturnedTask()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingTaskWithoutFallbackCommand
            {
                Riemann = mockRiemann.Object
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback InvokeAsync", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithoutFallback ExecuteInIsolation", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_GeneralExceptionFromExecute()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingExecuteWithoutFallbackCommand
            {
                Riemann = mockRiemann.Object
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback InvokeAsync", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithoutFallback ExecuteInIsolation", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_OperationCanceledException()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new TimingOutWithoutFallbackCommand(TimeSpan.FromMilliseconds(100))
            {
                Riemann = mockRiemann.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.GetBaseException() is OperationCanceledException);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback InvokeAsync", "Canceled", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.TimingOutWithoutFallback ExecuteInIsolation", "Canceled", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_RejectedException()
        {
            var mockRiemann = new Mock<IRiemann>();
            
            var mockBreaker = new Mock<ICircuitBreaker>();
            mockBreaker.Setup(m => m.IsAllowing()).Returns(false);

            // Will have been set by TestFixture constructor.
            Assert.True(new ConfigurableValue<bool>("mjolnir.useCircuitBreakers").Value);

            var command = new SuccessfulEchoCommandWithoutFallback("Test")
            {
                Riemann = mockRiemann.Object,
                CircuitBreaker = mockBreaker.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.GetBaseException() is CircuitBreakerRejectedException);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback InvokeAsync", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.SuccessfulEchoCommandWithoutFallback ExecuteInIsolation", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackThrowsNonInstigator()
        {
            var expected = new ExpectedTestException("foo");
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingTaskWithEchoThrowingFallbackCommand(expected)
            {
                Riemann = mockRiemann.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (ExpectedTestException e)
            {
                if (e != expected) throw;
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithEchoThrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackRethrowsInstigator()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingTaskWithInstigatorRethrowingFallbackCommand
            {
                Riemann = mockRiemann.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.IsFallbackImplemented);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithInstigatorRethrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackSucceeds()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingTaskWithSuccessfulFallbackCommand
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskWithSuccessfulFallback TryFallback", "Success", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndFallbackSucceeds()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingExecuteWithSuccessfulFallbackCommand
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteWithSuccessfulFallback TryFallback", "Success", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_ExecuteFaultsAndFallbackNotImplemented()
        {
            var exception = new ExpectedTestException("foo");
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingExecuteEchoCommandWithoutFallback(exception)
            {
                Riemann = mockRiemann.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                if (e.GetBaseException() != exception) throw;
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingExecuteEchoCommandWithoutFallback TryFallback", "NotImplemented", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();   
        }

        [Fact]
        public async Task InvokeAsync_TaskFaultsAndFallbackNotImplemented()
        {
            var exception = new ExpectedTestException("foo");
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingTaskEchoCommandWithoutFallback(exception)
            {
                Riemann = mockRiemann.Object,
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                if (e.GetBaseException() != exception) throw;
                mockRiemann.Verify(m => m.Elapsed("mjolnir command test.FaultingTaskEchoCommandWithoutFallback TryFallback", "NotImplemented", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_SuccessAndFallbackImplemented()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new SuccessfulEchoCommandWithFallback("foo")
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed(It.IsRegex(".*TryFallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>(), null, null, null), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_SuccessAndFallbackNotImplemented()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new SuccessfulEchoCommandWithoutFallback("foo")
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed(It.IsRegex(".*TryFallback.*"), It.IsAny<string>(), It.IsAny<TimeSpan>(), null, null, null), Times.Never);
        }
    }
}
