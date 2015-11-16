using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                var command = new NoOpOnThreadAsyncCommand();
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
                var command = new NoOpOnThreadAsyncCommand();
                var invoker = new CommandInvoker();
                var failureMode = OnFailure.Return;

                // This first call shouldn't throw, but the second one should.
                await invoker.InvokeAsync(command, failureMode);

                // The timeout shouldn't matter. The invoked-once check should be
                // one of the first validations performed.
                await Assert.ThrowsAsync(typeof(InvalidOperationException), () => invoker.InvokeAsync(command, failureMode));
            }
        }
    }

    internal class NoOpOnThreadAsyncCommand : AsyncCommand<bool>
    {
        public NoOpOnThreadAsyncCommand() : base("test", "test", TimeSpan.FromMilliseconds(1))
        { }
        
        protected internal override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    internal class NoOpOffThreadAsyncCommand : AsyncCommand<bool>
    {
        public NoOpOffThreadAsyncCommand() : base("test", "test", TimeSpan.FromMilliseconds(1))
        { }

        protected internal override async Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(10);
            return true;
        }
    }
}
