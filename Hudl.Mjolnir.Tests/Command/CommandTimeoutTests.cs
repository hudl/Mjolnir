using System;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandTimeoutTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(1000);

        // TODO Assert.Throws() won't work with async/await until xUnit 2.0.
        // See: http://stackoverflow.com/a/14103924/29995
        // See: http://xunit.codeplex.com/workitem/9799

        [Fact]
        public async Task InvokeAsync_WithTimeout_TimesOutAndThrowsCommandException()
        {
            var command = new TimingOutWithoutFallbackCommand(Timeout);
            try
            {
                await command.InvokeAsync();
            }
            catch (CommandFailedException e)
            {
                Assert.True(e.GetBaseException() is OperationCanceledException);
                return;
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public async Task InvokeAsync_UnderTimeout_DoesntTimeoutOrThrowException()
        {
            var command = new ImmediatelyReturningCommandWithoutFallback();
            
            // Shouldn't throw exception.
            await command.InvokeAsync();
        }
    }
}
