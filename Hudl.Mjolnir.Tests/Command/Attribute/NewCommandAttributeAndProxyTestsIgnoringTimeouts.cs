using System.Threading;
using Hudl.Mjolnir.Command.Attribute;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command.Attribute
{
    public sealed class NewCommandAttributeAndProxyTestsIgnoringTimeouts : TestFixtureIgnoreTimeouts
    {
        [Attributes.Command("foo", "bar", CancellableWithIgnoredTimeout.Timeout)]
        public interface ICancellableIgnoredTimeout
        {
            string CancellableMethod(CancellationToken token);
        }

        public class CancellableWithIgnoredTimeout : ICancellableIgnoredTimeout
        {
            public const int Timeout = 500;
            public CancellationToken TokenRecievedFromProxy { get; private set; }
            public bool CallMade { get; private set; }
            private readonly string _returnResult;

            public CancellableWithIgnoredTimeout(string returnResult)
            {
                _returnResult = returnResult;
            }

            public string CancellableMethod(CancellationToken token)
            {
                CallMade = true;
                TokenRecievedFromProxy = token;
                return _returnResult;
            }
        }

        public class CancellableWithOverrunnningMethodTimeoutsIgnored : ICancellableIgnoredTimeout
        {
            public string CancellableMethod(CancellationToken token)
            {
                Thread.Sleep(CancellableWithIgnoredTimeout.Timeout + 50);
                token.ThrowIfCancellationRequested();
                return string.Empty;
            }
        }

        [Fact]
        public void ProxyPassesNoneToMethod_WhenTimeoutsIgnored()
        {
            var expectedResult = "test";
            var classToProxy = new CancellableWithIgnoredTimeout(expectedResult);
            var proxy = CommandInterceptor.CreateProxy<ICancellableIgnoredTimeout>(classToProxy);
            // If we pass CancellationToken.None to the proxy then it should pass this along to the method call, rather than a CancellationToken with a timeout. This should
            // be the case because we've set the Command to ignore timeouts
            var result = proxy.CancellableMethod(CancellationToken.None);
            Assert.True(classToProxy.CallMade);
            Assert.Equal(classToProxy.TokenRecievedFromProxy, CancellationToken.None);
            Assert.Equal(expectedResult, result);
        }

        // The behaviour should be the same regardless of whether we're ignoring timeouts or not. We still should be able to pass custom tokens
        [Fact]
        public void ProxyStillPassesOnTokenToMethod_WhenTimeoutsAreIgnored()
        {
            var expectedResult = "test";
            var classToProxy = new CancellableWithIgnoredTimeout(expectedResult);
            var proxy = CommandInterceptor.CreateProxy<ICancellableIgnoredTimeout>(classToProxy);
            // If we pass CancellationToken.None to the proxy then it should pass a timeout tokem to the method call.
            var token = new CancellationTokenSource(500).Token;
            var result = proxy.CancellableMethod(token);
            Assert.True(classToProxy.CallMade);
            Assert.Equal(classToProxy.TokenRecievedFromProxy,token);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void MethodShouldNotTimeout_WhenTimeoutsAreIgnored()
        {
            var classToProxy = new CancellableWithOverrunnningMethodTimeoutsIgnored();
            var proxy = CommandInterceptor.CreateProxy<ICancellableIgnoredTimeout>(classToProxy);
            Assert.DoesNotThrow(() => proxy.CancellableMethod(CancellationToken.None));
        }
    }
}
