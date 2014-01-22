using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandTests
    {
        [Fact]
        public void Construct_ZeroTimeoutConfigValue_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ZeroTimeoutCommand();
            });
        }

        private class ZeroTimeoutCommand : Command<object>
        {
            public ZeroTimeoutCommand() : base(GroupKey.Named("Test"), TimeSpan.FromMilliseconds(0)) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<object>(new { });
            }
        }

        [Fact]
        public void Construct_NegativeTimeoutConfigValue_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new NegativeTimeoutCommand();
            });
        }

        private class NegativeTimeoutCommand : Command<object>
        {
            public NegativeTimeoutCommand() : base(GroupKey.Named("Test"), TimeSpan.FromMilliseconds(-1)) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult<object>(new { });
            }
        }

        [Fact]
        public async Task InvokeAsync_WhenCalledTwiceForTheSameInstance_ThrowsException()
        {
            var command = new SuccessfulEchoCommandWithoutFallback(new { });

            await command.InvokeAsync();

            try
            {
                await command.InvokeAsync(); // Should throw.
            }
            catch (InvalidOperationException e)
            {
                Assert.Equal("A command instance may only be invoked once", e.Message);
                return; // Expected
            }

            AssertX.FailExpectedException();
        }

        [Fact]
        public void Name_UsesAssemblyWithClassName()
        {
            var command = new NameTestCommand();

            Assert.Equal("Hudl.Mjolnir.Tests", GetType().Assembly.GetName().Name);
            Assert.Equal("Tests.NameTest", command.Name);
        }

        [Fact]
        public void Name_UsesLastAssemblyPart()
        {
            var command = new NameTestCommand();

            Assert.Equal("Hudl.Mjolnir.Tests", GetType().Assembly.GetName().Name);
            Assert.True(command.Name.StartsWith("Tests."));
        }

        [Fact]
        public void Name_StripsCommandFromClassName()
        {
            var command = new NameTestCommand();

            Assert.Equal("Hudl.Mjolnir.Tests", GetType().Assembly.GetName().Name);
            Assert.False(command.Name.EndsWith("Command"));
        }

        [Fact]
        public void Invoke_ReturnsResultSynchronously()
        {
            var expected = new { };
            var command = new SuccessfulEchoCommandWithoutFallback(expected);
            Assert.Equal(expected, command.Invoke());
        }

        [Fact]
        public void InvokeAsync_WhenUsingResult_DoesntDeadlock()
        {
            ConfigurationUtility.Init();

            var expected = new { };
            var command = new SuccessfulEchoCommandWithoutFallback(expected);

            var result = command.InvokeAsync().Result; // Will deadlock if we don't .ConfigureAwait(false) when awaiting.
            Assert.Equal(expected, result);
        }

        private sealed class NameTestCommand : BaseTestCommand<object>
        {
            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
