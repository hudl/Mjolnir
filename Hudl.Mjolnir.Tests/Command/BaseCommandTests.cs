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
    public class BaseCommandTests
    {
        public class Constructor : TestFixture
        {
            [Fact]
            public void NullOrEmptyGroup_Throws()
            {
                ArgumentNullException e;
                
                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(null, Rand.String(), Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("group", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand("", Rand.String(), Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("group", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(" ", Rand.String(), Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("group", e.ParamName);
            }

            [Fact]
            public void NullOrEmptyBreakerKey_Throws()
            {
                ArgumentNullException e;

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), null, Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("breakerKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), "", Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("breakerKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), " ", Rand.String(), TimeSpan.FromMilliseconds(1)));
                Assert.Equal("breakerKey", e.ParamName);
            }

            [Fact]
            public void NullOrEmptyBulkheadKey_Throws()
            {
                ArgumentNullException e;

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), Rand.String(), null, TimeSpan.FromMilliseconds(1)));
                Assert.Equal("bulkheadKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), Rand.String(), "", TimeSpan.FromMilliseconds(1)));
                Assert.Equal("bulkheadKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), Rand.String(), " ", TimeSpan.FromMilliseconds(1)));
                Assert.Equal("bulkheadKey", e.ParamName);
            }

            [Fact]
            public void NegativeOrZeroTimeout_Throws()
            {
                TimeSpan invalidTimeout;
                ArgumentException e;

                invalidTimeout = TimeSpan.FromMilliseconds(0);
                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), Rand.String(), Rand.String(), invalidTimeout));
                Assert.Equal("defaultTimeout", e.ParamName);

                invalidTimeout = TimeSpan.FromMilliseconds(-1);
                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(Rand.String(), Rand.String(), Rand.String(), invalidTimeout));
                Assert.Equal("defaultTimeout", e.ParamName);
            }

            [Fact]
            public void SetsGroupKey()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DifferentCommandInstancesUseCachedGroupKey()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void GeneratesName()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DifferentCommandInstancesUseCachedName()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void SetsBreakerKey()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DifferentCommandInstancesUseCachedBreakerKey()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void SetsBulkheadKey()
            {
                throw new NotImplementedException();
            }

            [Fact]
            public void DifferentCommandInstancesUseCachedBulkheadKey()
            {
                throw new NotImplementedException();
            }
        }


        private class TestCommand : BaseCommand
        {
            public TestCommand(string group, string breakerKey, string bulkheadKey, TimeSpan timeout) :
                base(group, breakerKey, bulkheadKey, timeout)
            { }
        }
    }

    public class OldBaseCommandTests : TestFixture
    {



        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);
        
        [Fact]
        public async Task InvokeAsync_WithTimeout_TimesOutAndThrowsCommandException()
        {
            var command = new TimingOutWithoutFallbackCommand(Timeout);

            var e = await Assert.ThrowsAsync<CommandTimeoutException>(() => command.InvokeAsync());
            Assert.True(e.GetBaseException() is OperationCanceledException);
        }
        
        [Fact]
        public async Task InvokeAsync_UnderTimeout_DoesntTimeoutOrThrowException()
        {
            var command = new ImmediatelyReturningCommandWithoutFallback();
            
            // Shouldn't throw exception.
            await command.InvokeAsync();
        }

        [Fact]
        public void Construct_TimeoutNotConfigured_UsesDefault()
        {
            var constructedTimeout = 17.Seconds();
            Assert.Null(new ConfigurableValue<object>("command.timeout-test.UnconfiguredTimeout.Timeout", null).Value);

            var command = new UnconfiguredTimeoutCommand(constructedTimeout);

            Assert.Equal(constructedTimeout, command.Timeout);
        }

        [Fact]
        public void Construct_TimeoutConfigured_UsesConfiguredValue()
        {
            var configuredTimeout = 12.Seconds();
            var constructedTimeout = configuredTimeout.Add(2.Seconds()); // Just needs to be different than configured value.
            ConfigProvider.Instance.Set("command.timeout-test.ConfiguredTimeout.Timeout", configuredTimeout.TotalMilliseconds);

            var command = new ConfiguredTimeoutCommand(constructedTimeout);

            Assert.Equal(configuredTimeout, command.Timeout);
        }

        [Fact]
        public void Construct_CachesAndReusesTimeoutConfigurableValue()
        {
            var command1 = new CachedTimeoutConfigurationCommand1();
            var command2 = new CachedTimeoutConfigurationCommand1();

            Assert.Equal(command1.GetCachedTimeout(), command2.GetCachedTimeout());
            
            var command3 = new CachedTimeoutConfigurationCommand2();

            Assert.NotEqual(command1.GetCachedTimeout(), command3.GetCachedTimeout());
        }

        private class UnconfiguredTimeoutCommand : BaseTestCommand<object>
        {
            // Use a dot in the group name to also test dot-dash conversion and that we're using the converted Name as the config value.
            public UnconfiguredTimeoutCommand(TimeSpan timeout) : base("timeout.test", "test", timeout) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class ConfiguredTimeoutCommand : BaseTestCommand<object>
        {
            // Use a dot in the group name to also test dot-dash conversion and that we're using the converted Name as the config value.
            public ConfiguredTimeoutCommand(TimeSpan timeout) : base("timeout.test", "test", timeout) {}

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private abstract class CachedTimeoutConfigurationCommand : BaseTestCommand<object>
        {
            internal IConfigurableValue<long> GetCachedTimeout()
            {
                return TimeoutConfigCache[Name];
            }

            protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        private class CachedTimeoutConfigurationCommand1 : CachedTimeoutConfigurationCommand
        {
            
        }

        private class CachedTimeoutConfigurationCommand2 : CachedTimeoutConfigurationCommand
        {

        }
    }
}
