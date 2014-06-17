using System;
using System.Threading;

namespace Hudl.Mjolnir.ThreadPool
{
    internal interface IWorkItem<TResult>
    {
        TResult Get(CancellationToken cancellationToken, TimeSpan timeout);
    }
}