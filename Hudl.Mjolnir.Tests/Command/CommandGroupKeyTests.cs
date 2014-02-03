using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandGroupKeyTests : TestFixture
    {
        [Fact]
        public void Construct_WithIsolationKey_UsesIsolationKeyAsAllKeys()
        {
            var command = new KeyTestCommand("test", "foo");

            Assert.Equal(GroupKey.Named("foo"), command.BreakerKey);
            Assert.Equal(GroupKey.Named("foo"), command.PoolKey);
        }

        [Fact]
        public void Construct_WithSpecificBreakerKey_UsesProvidedBreakerKey()
        {
            var command = new KeyTestCommand("test", "foo", "test");
            Assert.Equal(GroupKey.Named("foo"), command.BreakerKey);
        }

        [Fact]
        public void Construct_WithSpecificPoolKey_UsesProvidedPoolKey()
        {
            var command = new KeyTestCommand("test", "test", "bar");
            Assert.Equal(GroupKey.Named("bar"), command.PoolKey);
        }

        private sealed class KeyTestCommand : Command<object>
        {
            internal KeyTestCommand(string group, string isolationKey)
                : base(group, isolationKey, TimeSpan.FromMilliseconds(10000)) { }

            internal KeyTestCommand(string group, string breakerKey, string poolKey)
                : base(group, breakerKey, poolKey, TimeSpan.FromMilliseconds(10000)) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                // Doesn't matter, we won't execute it.
                throw new NotImplementedException();
            }
        }
    }
}
