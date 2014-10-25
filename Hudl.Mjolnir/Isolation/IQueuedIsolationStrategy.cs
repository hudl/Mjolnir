using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Isolation
{
    internal interface IQueuedIsolationStrategy
    {
        Task Enqueue(Action action, CancellationToken cancellationToken);
        Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken);
    }
}