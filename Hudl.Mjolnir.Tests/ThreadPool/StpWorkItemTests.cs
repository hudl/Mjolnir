using System;
using System.Diagnostics;
using System.Threading;
using Amib.Threading;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.ThreadPool;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.ThreadPool
{
    public class StpWorkItemTests : TestFixture
    {
        [Fact]
        public void Get_UnderlyingGetResultThrowsException_RethrowsWrapped()
        {
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
            catch (IsolationThreadPoolException e)
            {
                Debug.WriteLine("Trace: " + e);
                Assert.Equal(itemException, e.InnerException);
                Assert.Equal(cause, e.InnerException.InnerException);
                Assert.Equal(inner, e.InnerException.InnerException.InnerException);
                return; // Expected.
            }
            
            AssertX.FailExpectedException();
        }

        [Fact]
        public void Get_WhenCancellationTokenExpires_ThrowsCanceledExceptionAfterGetResultReturns()
        {
            var source = new CancellationTokenSource(500);
            var token = source.Token;
            var timeout = TimeSpan.FromSeconds(1);

            var mockWorkItemResult = new Mock<IWorkItemResult<object>>();
            mockWorkItemResult.Setup(m => m.GetResult(timeout, false)).Returns(new { });
            mockWorkItemResult.Setup(m => m.GetResult(timeout, false)).Returns(() =>
            {
                Thread.Sleep(1000); // 2x the token timeout.
                return new { };
            });

            var stpWorkItem = new StpWorkItem<object>(mockWorkItemResult.Object);

            try
            {
                stpWorkItem.Get(token, timeout);
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
