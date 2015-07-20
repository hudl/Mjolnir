using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class IgnoredTimeoutsCommand : BaseTestCommand<object>
    {
        public IgnoredTimeoutsCommand() : base(TimeSpan.FromMilliseconds(1000)){}

        protected override async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            //the cancellation token should be none here so it shouldn't throw if we go over the timeout
            var delay = Timeout.Add(TimeSpan.FromMilliseconds(100));

            return await Task.Run(() =>
            {
                Thread.Sleep(delay);
                cancellationToken.ThrowIfCancellationRequested();
                return 1;
            }, cancellationToken);
        }
    }
}
