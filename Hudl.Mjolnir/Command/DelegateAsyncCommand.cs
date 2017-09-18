using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    internal class DelegateAsyncCommand<TResult> : AsyncCommand<TResult>
    {
        private readonly Func<CancellationToken?, Task<TResult>> _func;

        public DelegateAsyncCommand(string group, Func<CancellationToken?, Task<TResult>> func) : base(group, group, TimeSpan.FromMilliseconds(2000))
        {
            _func = func ?? throw new ArgumentNullException(nameof(func));
        }

        public override Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            return _func(cancellationToken);
        }
    }
}