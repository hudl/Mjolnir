using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Isolation
{
    internal interface IQueuedIsolationStrategy
    {
        Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken);
    }
}