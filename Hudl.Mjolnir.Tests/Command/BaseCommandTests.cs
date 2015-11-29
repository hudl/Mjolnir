using System;
using Hudl.Config;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Tests.Command
{
    public class BaseCommandTests
    {
        private static readonly long ValidTimeoutMillis = 1000;
        private static readonly TimeSpan ValidTimeout = TimeSpan.FromMilliseconds(ValidTimeoutMillis);

        public class Constructor : TestFixture
        {
            [Fact]
            public void NullOrEmptyGroup_Throws()
            {
                ArgumentNullException e;
                
                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(null, AnyString, AnyString, ValidTimeout));
                Assert.Equal("group", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand("", AnyString, AnyString, ValidTimeout));
                Assert.Equal("group", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(" ", AnyString, AnyString, ValidTimeout));
                Assert.Equal("group", e.ParamName);
            }

            [Fact]
            public void NullOrEmptyBreakerKey_Throws()
            {
                ArgumentNullException e;

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, null, AnyString, ValidTimeout));
                Assert.Equal("breakerKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, "", AnyString, ValidTimeout));
                Assert.Equal("breakerKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, " ", AnyString, ValidTimeout));
                Assert.Equal("breakerKey", e.ParamName);
            }

            [Fact]
            public void NullOrEmptyBulkheadKey_Throws()
            {
                ArgumentNullException e;

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, AnyString, null, ValidTimeout));
                Assert.Equal("bulkheadKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, AnyString, "", ValidTimeout));
                Assert.Equal("bulkheadKey", e.ParamName);

                e = Assert.Throws<ArgumentNullException>(() => new TestCommand(AnyString, AnyString, " ", ValidTimeout));
                Assert.Equal("bulkheadKey", e.ParamName);
            }

            [Fact]
            public void NegativeOrZeroTimeout_Throws()
            {
                TimeSpan invalidTimeout;
                ArgumentException e;

                invalidTimeout = TimeSpan.FromMilliseconds(0);
                e = Assert.Throws<ArgumentException>(() => new TestCommand(AnyString, AnyString, AnyString, invalidTimeout));
                Assert.Equal("defaultTimeout", e.ParamName);

                invalidTimeout = TimeSpan.FromMilliseconds(-1);
                e = Assert.Throws<ArgumentException>(() => new TestCommand(AnyString, AnyString, AnyString, invalidTimeout));
                Assert.Equal("defaultTimeout", e.ParamName);
            }

            [Fact]
            public void SetsGroupKey()
            {
                var key = AnyString;
                var expected = GroupKey.Named(key);

                var command = new TestCommand(key, AnyString, AnyString, ValidTimeout);

                Assert.Equal(expected, command.Group);
            }
            
            [Fact]
            public void GeneratesNameWhenNameNotProvided()
            {
                // A name isn't normally provided to the constructor. It exists for things like the
                // [Command] attribute, which passed a name specific to the method it's proxying.
                //
                // The generated name is group-key.CommandClassName. Unit tests on the actual
                // generated values are in a different class. This just tests that we trigger the
                // generation during construction.

                var groupKey = "expected-group";

                var command = new TestCommand(groupKey, AnyString, AnyString, ValidTimeout);
                Assert.Equal(groupKey + ".Test", command.Name);
            }
            
            [Fact]
            public void SetsBreakerKey()
            {
                var key = AnyString;
                var expected = GroupKey.Named(key);

                var command = new TestCommand(AnyString, key, AnyString, ValidTimeout);

                Assert.Equal(expected, command.BreakerKey);
            }

            [Fact]
            public void SetsBulkheadKey()
            {
                var key = AnyString;
                var expected = GroupKey.Named(key);

                var command = new TestCommand(AnyString, AnyString, key, ValidTimeout);

                Assert.Equal(expected, command.BulkheadKey);
            }
        }

        public class DetermineTimeout : TestFixture
        {
            [Fact]
            public void InvocationTimeoutZero_UsesInvocationTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(0),
                    invocationMs: 0,
                    configuredMs: ValidTimeoutMillis,
                    constructorMs: ValidTimeoutMillis);
            }

            [Fact]
            public void InvocationTimeoutPositive_UsesInvocationTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(1),
                    invocationMs: 1,
                    configuredMs: ValidTimeoutMillis,
                    constructorMs: ValidTimeoutMillis);
            }

            [Fact]
            public void InvocationTimeoutNegative_FallsBackToConfiguredTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(2),
                    invocationMs: -1,
                    configuredMs: 2,
                    constructorMs: ValidTimeoutMillis);
            }

            [Fact]
            public void InvocationTimeoutNull_FallsBackToConfiguredTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(2),
                    invocationMs: null,
                    configuredMs: 2,
                    constructorMs: ValidTimeoutMillis);
            }

            [Fact]
            public void NoInvocationTimeout_ConfiguredTimeoutNegative_FallsBackToConstructorTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(3),
                    invocationMs: null,
                    configuredMs: -1,
                    constructorMs: 3);
            }

            [Fact]
            public void NoInvocationTimeout_ConfiguredTimeoutZero_FallsBackToConstructorTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(3),
                    invocationMs: null,
                    configuredMs: 0,
                    constructorMs: 3);
            }

            [Fact]
            public void NoInvocationTimeout_ConfiguredTimeoutPositive_UsesConfiguredTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(2),
                    invocationMs: null,
                    configuredMs: 2,
                    constructorMs: ValidTimeoutMillis);
            }

            [Fact]
            public void NoInvocationTimeout_NoConfiguredTimeout_UsesConstructorTimeout()
            {
                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(3),
                    invocationMs: null,
                    configuredMs: 0, // 0 = not configured
                    constructorMs: 3);
            }

            [Fact]
            public void NoInvocationTimeout_NoConfiguredTimeout_NoConstructorTimeout_UsesDefaultTimeout()
            {
                // 2 seconds is the system-wide default timeout

                TestTimeouts(
                    expected: TimeSpan.FromMilliseconds(2000),
                    invocationMs: null,
                    configuredMs: 0, // 0 = not configured
                    constructorMs: null);
            }

            private void TestTimeouts(TimeSpan expected, long? invocationMs, long configuredMs, long? constructorMs)
            {
                // The 'configured' value can't be nullable because the Hudl.Config library may pass
                // along a default(long) (i.e. 0) if the config value isn't set, and we need to be
                // prepared for that and not treat it as an actual timeout.

                var constructorTs = (constructorMs == null ? (TimeSpan?) null : TimeSpan.FromMilliseconds(constructorMs.Value));
                var command = new TestCommand(AnyString, AnyString, AnyString, constructorTs);
                ConfigProvider.Instance.Set("mjolnir.command." + command.Name + ".Timeout", configuredMs);

                var determined = command.DetermineTimeout(invocationMs);

                Assert.Equal(expected, determined);
            }
        }

        public class Name : TestFixture
        {
            // TODO assert that we use the generated name cache
            // TODO tests for various command classes and groups, validating name normalization
        }

        private class TestCommand : BaseCommand
        {
            public TestCommand(string group, string breakerKey, string bulkheadKey, TimeSpan? timeout) :
                base(group, breakerKey, bulkheadKey, timeout)
            { }
        }
    }
}
