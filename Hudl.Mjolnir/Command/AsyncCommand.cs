using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    public abstract class AsyncCommand<TResult> : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        // TODO do these have to be protected internal? maybe try reworking with some interfaces?
        protected internal abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
