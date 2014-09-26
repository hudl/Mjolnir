using System;
using System.Threading;
using Hudl.Mjolnir.Isolation;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class AlwaysSuccessfulIsolationThreadPool : IQueuedIsolationStrategy
    {
        public void Start()
        {
            // No-op.
        }

        public IWorkItem<TResult> Enqueue<TResult>(Func<TResult> func)
        {
            return new PassThroughWorkItem<TResult>(func());
        }
    }

    internal class PassThroughWorkItem<TResult> : IWorkItem<TResult>
    {
        private readonly TResult _result;

        public PassThroughWorkItem(TResult result)
        {
            _result = result;
        }

        public TResult Get(CancellationToken cancellationToken, TimeSpan timeout)
        {
            return _result;
        }
    }
}
