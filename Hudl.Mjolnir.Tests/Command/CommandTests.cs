using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Tests.TestCommands;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandTests : TestFixture
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
            public ZeroTimeoutCommand() : base("test", "test", "test", TimeSpan.Zero) {}

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
            public NegativeTimeoutCommand() : base("test", "test", "test", TimeSpan.FromMilliseconds(-1)) {}

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
        public void Name_UsesGroupAndClassName()
        {
            var command = new NameTestCommand("test");

            Assert.Equal("test.NameTest", command.Name);
        }

        [Fact]
        public void Name_StripsCommandFromClassName()
        {
            var command = new NameTestCommand("test");

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
        public void InvokeAsync_WhenUsingDotResult_DoesntDeadlock()
        {
            ConfigurationUtility.Init();

            var expected = new { };
            var command = new SuccessfulEchoCommandWithoutFallback(expected);

            var result = command.InvokeAsync().Result; // Will deadlock if we don't .ConfigureAwait(false) when awaiting.
            Assert.Equal(expected, result);
        }

        [Fact]
        public void InvokeAsync_WhenExceptionThrown_HasExpectedExceptionInsideAggregateException()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<AggregateException>(() => {
                var foo = command.InvokeAsync().Result;
            });

            // AggregateException -> CommandFailedException -> ExpectedTestException
            Assert.Equal(expected, result.InnerException.InnerException);
        }

        [Fact]
        public void Invoke_PropagatesExceptions()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<CommandFailedException>(() =>
            {
                command.Invoke();
            });

            Assert.Equal(expected, result.InnerException);
        }

        private sealed class NameTestCommand : BaseTestCommand<object>
        {
            public NameTestCommand(string group) : base(group, "asdf", 1.Seconds()) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
