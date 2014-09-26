using System;
using System.Threading;

namespace Hudl.Mjolnir.Isolation
{
    internal interface IWorkItem<TResult>
    {
        TResult Get(CancellationToken cancellationToken, TimeSpan timeout);
    }
}