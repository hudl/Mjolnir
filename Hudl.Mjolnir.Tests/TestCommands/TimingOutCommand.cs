using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Executes a task that delays one second longer than the timeout for this Command allows.
    /// </summary>
    internal class TimingOutWithoutFallbackCommand : BaseTestCommand<object>
    {
        internal TimingOutWithoutFallbackCommand() : base(TimeSpan.FromMilliseconds(100)) {}
        internal TimingOutWithoutFallbackCommand(TimeSpan timeout) : base(timeout) {}

        protected override async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            // Sleep for a second more than the timeout allows.
            var delay = Timeout.Add(TimeSpan.FromSeconds(1));

            return await Task.Run(() =>
                {
                    Thread.Sleep(delay);
                    cancellationToken.ThrowIfCancellationRequested();
                    return 1;
                }, cancellationToken);
        }
    }
}