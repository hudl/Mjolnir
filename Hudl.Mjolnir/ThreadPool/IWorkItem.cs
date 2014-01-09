using System;
using System.Threading;

namespace Hudl.Mjolnir.ThreadPool
{
    public interface IWorkItem<TResult>
    {
        TResult Get(CancellationToken cancellationToken, TimeSpan timeout);
    }
}