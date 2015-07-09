using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Command.Attribute;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command.Attribute
{
    public sealed class NewCommandAttributeAndProxyTests : TestFixture
    {
        [Attributes.Command("foo", "bar",true)]
        public interface ICancellableIgnoredTimeout
        {
            string CancellableMethod(CancellationToken token);
        }

        [Attributes.Command("foo","bar",CancellableWithTimeoutPreserved.Timeout)] //using a 1s timeout rather than the default
        public interface ICancellableTimeoutPreserved
        {
            string CancellableMethod(CancellationToken token);
        }

        public class CancellableWithIgnoredTimeout : ICancellableIgnoredTimeout
        {
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

        public class CancellableWithTimeoutPreserved : ICancellableTimeoutPreserved
        {
            public const int Timeout = 1000;
            public CancellationToken TokenRecievedFromProxy { get; private set; }
            public bool CallMade { get; private set; }
            private readonly string _returnResult;
            public CancellableWithTimeoutPreserved(string returnResult)
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

        public class CancellableWithOverrunnningMethod : ICancellableTimeoutPreserved
        {
            public string CancellableMethod(CancellationToken token)
            {
                Thread.Sleep(CancellableWithTimeoutPreserved.Timeout+50);
                token.ThrowIfCancellationRequested();
                return string.Empty;
            }
        }

        public class CancellableWithOverrunnningMethodTimeoutsIgnored : ICancellableIgnoredTimeout
        {
            public string CancellableMethod(CancellationToken token)
            {
                Thread.Sleep(CancellableWithTimeoutPreserved.Timeout + 50);
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
            //If we pass CancellationToken.None to the proxy then it should pass this along to the method call, rather than a CancellationToken with a timeout. This should
            //be the case because we've set the Command to ignore timeouts
            var result = proxy.CancellableMethod(CancellationToken.None);
            Assert.True(classToProxy.CallMade && classToProxy.TokenRecievedFromProxy==CancellationToken.None);
            Assert.Equal(expectedResult,result);
        }

        //the inverse of the test above. Want to make sure that a CancellationToken with timeout is passed by the proxy when we haven't flagged the command as having timeouts ignored. 
        [Fact]
        public void ProxyPassesATimeoutTokenToMethod_WhenTimeoutsNotIgnored()
        {
            var expectedResult = "test";
            var classToProxy = new CancellableWithTimeoutPreserved(expectedResult);
            var proxy = CommandInterceptor.CreateProxy<ICancellableTimeoutPreserved>(classToProxy);
            //If we pass CancellationToken.None to the proxy then it should pass a timeout tokem to the method call.
            var result = proxy.CancellableMethod(CancellationToken.None);
            Assert.True(classToProxy.CallMade && classToProxy.TokenRecievedFromProxy != CancellationToken.None);
            //shouldn't be cancelled yet
            Assert.False(classToProxy.TokenRecievedFromProxy.IsCancellationRequested);
            Thread.Sleep(CancellableWithTimeoutPreserved.Timeout+50); //sleep past the timeout 
            Assert.True(classToProxy.TokenRecievedFromProxy.IsCancellationRequested);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void ProxyPassesOnTokenToMethod_WhenTimeoutsNotIgnored()
        {
            var expectedResult = "test";
            var classToProxy = new CancellableWithTimeoutPreserved(expectedResult);
            var proxy = CommandInterceptor.CreateProxy<ICancellableTimeoutPreserved>(classToProxy);
            //If we pass a valid token to the proxy then it should pass the token to the method call.
            var token = new CancellationTokenSource(500).Token;
            var result = proxy.CancellableMethod(token);
            Assert.True(classToProxy.CallMade);
            Assert.Equal(classToProxy.TokenRecievedFromProxy,token);
            Assert.Equal(expectedResult, result);
        }

        //same as the test above. The behaviour should be the same regardless of whether we're ignoring timeouts or not. We still should be able to pass custom tokens
        [Fact]
        public void ProxyStillPassesOnTokenToMethod_WhenTimeoutsAreIgnored()
        {
            var expectedResult = "test";
            var classToProxy = new CancellableWithIgnoredTimeout(expectedResult);
            var proxy = CommandInterceptor.CreateProxy<ICancellableIgnoredTimeout>(classToProxy);
            //If we pass CancellationToken.None to the proxy then it should pass a timeout tokem to the method call.
            var token = new CancellationTokenSource(500).Token;
            var result = proxy.CancellableMethod(token);
            Assert.True(classToProxy.CallMade);
            Assert.Equal(classToProxy.TokenRecievedFromProxy,token);
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void MethodShouldTimeout_WhenTimeoutsAreNotIgnored()
        {
            var classToProxy = new CancellableWithOverrunnningMethod();
            var proxy = CommandInterceptor.CreateProxy<ICancellableTimeoutPreserved>(classToProxy);
            Assert.Throws<CommandFailedException>(()=>proxy.CancellableMethod(CancellationToken.None));
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
