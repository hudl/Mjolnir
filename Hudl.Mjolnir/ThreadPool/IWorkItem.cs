using System;
using System.Threading;

namespace Hudl.Mjolnir.ThreadPool
{
    public interface IWorkItem<TResult> // TODO make this internal.
    {
        TResult Get(CancellationToken cancellationToken, TimeSpan timeout);
    }
}