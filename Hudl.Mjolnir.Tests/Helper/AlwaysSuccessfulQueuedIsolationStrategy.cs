using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Isolation;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class AlwaysSuccessfulQueuedIsolationStrategy : IQueuedIsolationStrategy
    {
        public Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken)
        {
            // Use the default scheduler, which doesn't enforce concurrency.
            return Task.Factory.StartNew(func, cancellationToken);
        }
    }
}
