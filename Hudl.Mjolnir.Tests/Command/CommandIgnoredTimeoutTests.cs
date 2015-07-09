using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public sealed class CommandIgnoredTimeoutTests : TestFixtureIgnoreTimeouts
    {
        [Fact]
        public async Task InvokeAsync_WithTimeoutsIgnored_TimeoutShouldElapseButWithoutAnException()
        {
            var command = new IgnoredTimeoutsCommand();
            await command.InvokeAsync(); // shouln't get an exception here
        }
    }
}
