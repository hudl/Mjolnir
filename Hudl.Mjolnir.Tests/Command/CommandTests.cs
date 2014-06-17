using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
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
        public void Name_Generated_UsesGroupAndClassName()
        {
            var command = new GeneratedNameTestCommand("test");

            Assert.Equal("test.GeneratedNameTest", command.Name);
        }

        [Fact]
        public void Name_Generated_StripsCommandFromClassName()
        {
            var command = new GeneratedNameTestCommand("test");

            Assert.False(command.Name.EndsWith("Command"));
        }

        /// <summary>
        /// For consistency, we'll try to keep Command names with two parts,
        /// separated by a dot (i.e. group.ClassName). If groups get provided
        /// with dots, replace them with dashes to maintain that consistency.
        /// This also helps out with Graphite namespacing, which also uses
        /// dots as delimiters.
        /// </summary>
        [Fact]
        public void Name_Generated_ReplacesDotsWithDashesInGroup()
        {
            var command = new GeneratedNameTestCommand("test.test");

            Assert.Equal("test-test.GeneratedNameTest", command.Name);
        }

        [Fact]
        public void Name_Generated_IsCached()
        {
            var type = typeof (GeneratedNameCacheTestCommand);
            var command1 = new GeneratedNameCacheTestCommand("test");

            var cachedName = command1.GetCachedName(type);
            Assert.Equal(command1.Name, cachedName);

            var command2 = new GeneratedNameCacheTestCommand("test");

            Assert.True(ReferenceEquals(command1.Name, command2.Name));
        }

        [Fact]
        public void Name_Generated_TwoCommandsWithDifferentGroups_ReturnsCorrectNameForEach()
        {
            var type = typeof (GeneratedNameTestCommand);
            var command1 = new GeneratedNameCacheTestCommand("test-one");
            var command2 = new GeneratedNameCacheTestCommand("test-two");

            Assert.Equal("test-one.GeneratedNameCacheTest", command1.Name);
            Assert.Equal("test-two.GeneratedNameCacheTest", command2.Name);
        }

        [Fact]
        public void Name_Provided_IsUsedInsteadOfGeneratedName()
        {
            var command = new ProvidedNameTestCommand("my-group", "FooBarBaz");
            Assert.Equal("my-group.FooBarBaz", command.Name);
        }

        [Fact]
        public void Name_Provided_DoesntHaveCommandStripped()
        {
            var command = new ProvidedNameTestCommand("my-group", "FooBarBazCommand");
            Assert.Equal("my-group.FooBarBazCommand", command.Name);
        }

        [Fact]
        public void Name_Provided_ReplacesDotsWithDashesInGroup()
        {
            var command = new ProvidedNameTestCommand("my.group", "FooBarBaz");
            Assert.Equal("my-group.FooBarBaz", command.Name);
        }

        [Fact]
        public void Name_Provided_ReplacesDotsWithDashesInName()
        {
            var command = new ProvidedNameTestCommand("my-group", "FooBar.Baz");
            Assert.Equal("my-group.FooBar-Baz", command.Name);
        }

        [Fact]
        public void Name_Provided_IsCached()
        {
            const string name = "QuxQuuxCorge";
            var command1 = new ProvidedNameTestCommand("test-group", name);

            var cachedName = command1.GetCachedName(name);
            Assert.Equal(command1.Name, cachedName);

            var command2 = new ProvidedNameTestCommand("test-group", name);

            Assert.True(ReferenceEquals(command1.Name, command2.Name));
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
            var expected = new { };
            var command = new SuccessfulEchoCommandWithoutFallback(expected);

            var result = command.InvokeAsync().Result; // Will deadlock if we don't .ConfigureAwait(false) when awaiting.
            Assert.Equal(expected, result);
        }

        [Fact]
        public void InvokeAsync_WhenExceptionThrownFromExecute_HasExpectedExceptionInsideAggregateException()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingExecuteEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<AggregateException>(() => {
                var foo = command.InvokeAsync().Result;
            });

            // AggregateException -> CommandFailedException -> ExpectedTestException
            Assert.Equal(expected, result.InnerException.InnerException);
        }

        [Fact]
        public void InvokeAsync_WhenExceptionThrownFromTask_HasExpectedExceptionInsideAggregateException()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingTaskEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<AggregateException>(() =>
            {
                var foo = command.InvokeAsync().Result;
            });

            // AggregateException -> CommandFailedException -> ExpectedTestException
            Assert.Equal(expected, result.InnerException.InnerException);
        }

        [Fact]
        public void Invoke_FaultingExecute_PropagatesException()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingExecuteEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<CommandFailedException>(() =>
            {
                command.Invoke();
            });

            Assert.Equal(expected, result.InnerException);
        }

        [Fact]
        public void Invoke_FaultingTask_PropagatesException()
        {
            var expected = new ExpectedTestException("Exception");
            var command = new FaultingTaskEchoCommandWithoutFallback(expected);

            var result = Assert.Throws<CommandFailedException>(() =>
            {
                command.Invoke();
            });

            Assert.Equal(expected, result.InnerException);
        }

        private sealed class GeneratedNameTestCommand : BaseTestCommand<object>
        {
            public GeneratedNameTestCommand(string group) : base(group, "asdf", 1.Seconds()) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class GeneratedNameCacheTestCommand : BaseTestCommand<object>
        {
            public GeneratedNameCacheTestCommand(string group) : base(group, "test", 1.Seconds()) {}

            public string GetCachedName(Type type)
            {
                return GeneratedNameCache[new Tuple<Type, GroupKey>(type, Group)];
            }

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ProvidedNameTestCommand : BaseTestCommand<object>
        {
            public ProvidedNameTestCommand(string group, string name) : base(group, name, "test", 1.Seconds()) {}

            public string GetCachedName(string name)
            {
                return ProvidedNameCache[new Tuple<string, GroupKey>(name, Group)];
            }

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
