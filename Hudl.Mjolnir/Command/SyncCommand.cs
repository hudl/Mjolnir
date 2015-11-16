using System;
using System.Threading;

namespace Hudl.Mjolnir.Command
{
    public abstract class SyncCommand<TResult> : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract TResult Execute(CancellationToken cancellationToken);
    }

    //public abstract class SyncCommand : BaseCommand<TResult>
    //{
    //    public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
    //    { }
    //    protected internal abstract void Execute(CancellationToken cancellationToken);
    //}
}
