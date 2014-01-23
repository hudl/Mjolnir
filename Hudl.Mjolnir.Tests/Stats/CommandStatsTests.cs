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

            mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.ImmediatelyReturningCommandWithoutFallback InvokeAsync", "RanToCompletion", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
            mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.ImmediatelyReturningCommandWithoutFallback ExecuteInIsolation", "RanToCompletion", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_GeneralException()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingWithoutFallbackCommand
            {
                Riemann = mockRiemann.Object
            };

            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException)
            {
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.FaultingWithoutFallback InvokeAsync", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.FaultingWithoutFallback ExecuteInIsolation", "Faulted", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
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
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.TimingOutWithoutFallback InvokeAsync", "Canceled", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.TimingOutWithoutFallback ExecuteInIsolation", "Canceled", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
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
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.SuccessfulEchoCommandWithoutFallback InvokeAsync", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.SuccessfulEchoCommandWithoutFallback ExecuteInIsolation", "Rejected", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackThrowsNonInstigator()
        {
            var expected = new ExpectedTestException("foo");
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingWithEchoThrowingFallbackCommand(expected)
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
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.FaultingWithEchoThrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackRethrowsInstigator()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingWithInstigatorRethrowingFallbackCommand
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
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.FaultingWithInstigatorRethrowingFallback TryFallback", "Failure", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackSucceeds()
        {
            var mockRiemann = new Mock<IRiemann>();
            var command = new FaultingWithSuccessfulFallbackCommand
            {
                Riemann = mockRiemann.Object,
            };

            await command.InvokeAsync();

            mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.FaultingWithSuccessfulFallback TryFallback", "Success", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_FaultsAndFallbackNotImplemented()
        {
            var exception = new ExpectedTestException("foo");
            var mockRiemann = new Mock<IRiemann>();
            var command = new EchoThrowingCommandWithoutFallback(exception)
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
                mockRiemann.Verify(m => m.Elapsed("mjolnir command Tests.EchoThrowingCommandWithoutFallback TryFallback", "NotImplemented", It.IsAny<TimeSpan>(), null, null, null), Times.Once);
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
