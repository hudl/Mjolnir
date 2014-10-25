using System;
using System.Threading;
using Amib.Threading;
using Hudl.Mjolnir.Isolation;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Isolation
{
    public class StpWorkItemTests : TestFixture
    {
        [Fact]
        public void Get_UnderlyingGetResultThrowsException_RethrowsInnerException()
        {
            // To avoid leaking STP's WorkItemResultException out beyond our thread pooling layer,
            // our wrappers around STP should unwrap the exception thrown by the pool and just
            // rethrow its inner exception, which should be what got thrown from ExecuteAsync().

            var inner = new ExpectedTestException("Root cause inner");
            var cause = new ExpectedTestException("Test root cause", inner);
            var itemException = new WorkItemResultException("Work item result exception", cause);

            var timeout = TimeSpan.FromSeconds(1);

            var mockWorkItemResult = new Mock<IWorkItemResult<object>>();
            mockWorkItemResult.Setup(m => m.GetResult(timeout, false)).Throws(itemException);

            var stpWorkItem = new StpWorkItem<object>(mockWorkItemResult.Object);

            try
            {
                stpWorkItem.Get(new CancellationToken(), timeout);
            }
            catch (ExpectedTestException e)
            {
                Assert.Equal(cause, e);
                Assert.Equal(inner, e.InnerException);
                return; // Expected.
            }
            
            AssertX.FailExpectedException();
        }

        [Fact]
        public void Get_WhenCancellationTokenExpires_ThrowsCanceledExceptionAfterGetResultReturns()
        {
            const int timeoutMillis = 100;

            var source = new CancellationTokenSource(timeoutMillis);
            var token = source.Token;
            var workItemTimeout = TimeSpan.FromSeconds(timeoutMillis * 10); // Doesn't really matter, we shouldn't hit it. Needs to be greater than the token timeout.

            var mockWorkItemResult = new Mock<IWorkItemResult<object>>();
            mockWorkItemResult.Setup(m => m.GetResult(workItemTimeout, false)).Returns(() =>
            {
                Thread.Sleep(timeoutMillis * 2);
                return new { };
            });

            var stpWorkItem = new StpWorkItem<object>(mockWorkItemResult.Object);

            try
            {
                stpWorkItem.Get(token, workItemTimeout);
            }
            catch (OperationCanceledException e)
            {
                Assert.Equal(token, e.CancellationToken);
                return; // Expected.
            }

            AssertX.FailExpectedException();
        }
    }
}
