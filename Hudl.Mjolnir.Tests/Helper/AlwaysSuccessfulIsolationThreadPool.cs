using System;
using System.Threading;
using Hudl.Mjolnir.ThreadPool;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class AlwaysSuccessfulIsolationThreadPool : IIsolationThreadPool
    {
        public void Start()
        {
            // No-op.
        }

        public IWorkItem<TResult> Enqueue<TResult>(Func<TResult> func)
        {
            return new PassThroughWorkItem<TResult>(func());
        }

        public string Name { get { return "always-successful"; } }
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
