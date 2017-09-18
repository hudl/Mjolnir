using System;
using System.Threading;

namespace Hudl.Mjolnir.Command
{
    internal class DelegateSyncCommand<TResult> : SyncCommand<TResult>
    {
        private readonly Func<CancellationToken?, TResult> _func;

        public DelegateSyncCommand(string group, Func<CancellationToken?, TResult> func) : base(group, group, TimeSpan.FromMilliseconds(2000))
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override TResult Execute(CancellationToken cancellationToken)
        {
            return _func(cancellationToken);
        }
    }
}
