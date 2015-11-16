using Hudl.Mjolnir.Command;
using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.External;
using Moq;
using Xunit;
using System.Threading;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandInvokerTests
    {
        public class InvokeAsync : TestFixture
        {
            [Fact]
            public async Task WhenCommandAlreadyInvoked_FailureModeIsThrow_Throws()
            {
                // A command instance should only be invoked once. The failure mode
                // here shouldn't actually matter; there's a similar test below
                // that asserts the same throwing behavior here with a different
                // failure mode.

                // The command used here doesn't matter.
                var command = new NoOpAsyncCommand();
                var invoker = new CommandInvoker();
                var failureMode = OnFailure.Throw;

                // This first call shouldn't throw, but the second one should.
                await invoker.InvokeAsync(command, failureMode);

                // The timeout shouldn't matter. The invoked-once check should be
                // one of the first validations performed.
                await Assert.ThrowsAsync(typeof(InvalidOperationException), () => invoker.InvokeAsync(command, failureMode));
            }

            [Fact]
            public async Task WhenCommandAlreadyInvoked_FailureModeIsReturn_StillThrows()
            {
                // Even if the failure mode isn't "Throw", we want to throw in
                // this situation. This is a bug on the calling side, since Command
                // instances shouldn't be reused. The exception thrown should help
                // the caller see that problem and fix it.

                // The command used here doesn't matter.
                var command = new NoOpAsyncCommand();
                var invoker = new CommandInvoker();
                var failureMode = OnFailure.Return;

                // This first call shouldn't throw, but the second one should.
                await invoker.InvokeAsync(command, failureMode);

                // The timeout shouldn't matter. The invoked-once check should be
                // one of the first validations performed.
                await Assert.ThrowsAsync(typeof(InvalidOperationException), () => invoker.InvokeAsync(command, failureMode));
            }

            // TODO test that the two non-CancellationToken overloads create the right cancellation tokens; after that, using the Token overload should be safe

            [Fact]
            public async Task SuccessfulExecution_FailureModeThrow_ReturnsWrappedResult()
            {
                // Successful command execution should return a wrapped CommandResult.

                var expectedTimeoutUsed = TimeSpan.FromSeconds(10);
                const bool expectedResultValue = true;
                var command = new NoOpAsyncCommand(expectedTimeoutUsed);
                
                var mockStats = new Mock<IStats>();
                var mockBulkheadInvoker = new Mock<IBulkheadInvoker>();
                mockBulkheadInvoker.Setup(m => m.ExecuteWithBulkheadAsync(command, It.IsAny<CancellationToken>())).Returns(Task.FromResult(expectedResultValue));

                var invoker = new CommandInvoker(mockStats.Object, mockBulkheadInvoker.Object);

                // We're testing OnFailure.Throws here. Mainly, it shouldn't throw if we're successful.
                var result = await invoker.InvokeAsync(command, OnFailure.Throw);

                mockBulkheadInvoker.Verify(m => m.ExecuteWithBulkheadAsync(command, It.IsAny<CancellationToken>())); // TODO test CancellationToken more accurately?
                mockStats.Verify(m => m.Elapsed("mjolnir command test.NoOpAsync execute", "RanToCompletion", It.IsAny<TimeSpan>()));
                Assert.Equal(CommandCompletionStatus.RanToCompletion, result.Status);
                Assert.Null(result.Exception);
                Assert.Equal(true, result.Value);
            }

            [Fact]
            public async Task SuccessfulExecution_FailureModeReturn_ReturnsWrappedResult()
            {
                // Successful command execution should return a wrapped CommandResult.

                var expectedTimeoutUsed = TimeSpan.FromSeconds(10);
                const bool expectedResultValue = true;
                var command = new NoOpAsyncCommand(expectedTimeoutUsed);

                var mockStats = new Mock<IStats>();
                var mockBulkheadInvoker = new Mock<IBulkheadInvoker>();
                mockBulkheadInvoker.Setup(m => m.ExecuteWithBulkheadAsync(command, It.IsAny<CancellationToken>())).Returns(Task.FromResult(expectedResultValue));

                var invoker = new CommandInvoker(mockStats.Object, mockBulkheadInvoker.Object);

                // We're testing OnFailure.Return here. The failure mode shouldn't have any bearing
                // on what happens during successful execution.
                var result = await invoker.InvokeAsync(command, OnFailure.Return);

                mockBulkheadInvoker.Verify(m => m.ExecuteWithBulkheadAsync(command, It.IsAny<CancellationToken>())); // TODO test CancellationToken more accurately?
                mockStats.Verify(m => m.Elapsed("mjolnir command test.NoOpAsync execute", "RanToCompletion", It.IsAny<TimeSpan>()));
                Assert.Equal(CommandCompletionStatus.RanToCompletion, result.Status);
                Assert.Null(result.Exception);
                Assert.Equal(true, result.Value);
            }



            // cancel + throw failureaction
            // cancel + return failureaction
            // fault + throw failureaction
            // fault + return failureaction
            // reject + throw failureaction
            // reject + return failureaction

            // different timeout values and configs
            
            // for each test:
            // - assert "Invoke" log written
            // - assert bulkhead called appropriately
            // - assert stat written
            // - assert exception data is correct
            // - if returning, assert result properties

        }
    }

    // Async command that does nothing, and doesn't actually go async - it wraps a synchronous
    // result and returns. Use this when you don't care what the Command actually does.
    internal class NoOpAsyncCommand : AsyncCommand<bool>
    {
        public NoOpAsyncCommand() : this(TimeSpan.FromSeconds(10)) { }
        public NoOpAsyncCommand(TimeSpan timeout) : base("test", "test", timeout) { }
        
        protected internal override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    //// Async command that does nothing, but goes async (off the current thread) for a short while
    //// using a Task.Delay(). Use this when you don't care what the Command actually does.
    //internal class NoOpOffThreadAsyncCommand : AsyncCommand<bool>
    //{
    //    public NoOpOffThreadAsyncCommand() : base("test", "test", TimeSpan.FromMilliseconds(10000))
    //    { }

    //    protected internal override async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    //    {
    //        await Task.Delay(10, cancellationToken);
    //        return true;
    //    }
    //}

    //// Async command that's successful (i.e. doesn't fault). Doesn't actually go async; it wraps a
    //// synchronous result and returns it.
    //internal class SuccessfulOnThreadAsyncCommand : AsyncCommand<bool>
    //{
    //    public SuccessfulOnThreadAsyncCommand() : base("test", "test", TimeSpan.FromMilliseconds(10000))
    //    { }
        
    //    protected internal override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.FromResult(true);
    //    }
    //}

    //// Async command that's successful (i.e. doesn't fault). Goes async off the current thread for
    //// a short while using a Task.Delay().
    //internal class SuccessfulOffThreadAsyncCommand : AsyncCommand<bool>
    //{
    //    public SuccessfulOffThreadAsyncCommand() : base("test", "test", TimeSpan.FromMilliseconds(10000))
    //    { }

    //    protected internal override async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    //    {
    //        await Task.Delay(10, cancellationToken);
    //        return true;
    //    }
    //}
}
