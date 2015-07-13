using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public sealed class CommandIgnoredTimeoutTests : TestFixture
    {
        [Fact]
        public async Task InvokeAsync_WithTimeoutsIgnored_TimeoutShouldElapseButWithoutAnException()
        {
            ConfigProvider.Instance.Set(IgnoreTimeoutsKey, true);
            var command = new IgnoredTimeoutsCommand();
            // Shouln't get an exception here.
            await command.InvokeAsync(); 
            ConfigProvider.Instance.Set(IgnoreTimeoutsKey, false);
        }
    }
}
