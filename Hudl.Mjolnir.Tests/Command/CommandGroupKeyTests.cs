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
        public void Construct_WithoutAnyKeys_UsesGroupKeyAsAllKeys()
        {
            var command = new KeyTestCommand(GroupKey.Named("Foo"));

            Assert.Equal(GroupKey.Named("Foo"), command.BreakerKey);
            Assert.Equal(GroupKey.Named("Foo"), command.PoolKey);
        }

        [Fact]
        public void Construct_WithBreakerKey_UsesProvidedBreakerKey()
        {
            var command = new KeyTestCommand(GroupKey.Named("Foo"), GroupKey.Named("Test"));
            Assert.Equal(GroupKey.Named("Foo"), command.BreakerKey);
        }

        [Fact]
        public void Construct_WithPoolKey_UsesProvidedPoolKey()
        {
            var command = new KeyTestCommand(GroupKey.Named("Test"), GroupKey.Named("Bar"));
            Assert.Equal(GroupKey.Named("Bar"), command.PoolKey);
        }

        private sealed class KeyTestCommand : Command<object>
        {
            internal KeyTestCommand(GroupKey isolationKey)
                : base(isolationKey, TimeSpan.FromMilliseconds(10000)) { }

            internal KeyTestCommand(GroupKey breakerKey, GroupKey poolKey)
                : base(breakerKey, poolKey, TimeSpan.FromMilliseconds(10000)) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                // Doesn't matter, we won't execute it.
                throw new NotImplementedException();
            }
        }
    }
}
