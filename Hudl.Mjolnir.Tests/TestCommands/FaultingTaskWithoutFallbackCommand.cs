using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Throws a NotImplementedException from the Task returned by ExecuteAsync.
    /// </summary>
    internal class FaultingTaskWithoutFallbackCommand : BaseTestCommand<object>
    {
        internal FaultingTaskWithoutFallbackCommand() { }
        internal FaultingTaskWithoutFallbackCommand(TimeSpan timeout) : base(timeout) { }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(new Func<object>(() =>
            {
                throw new ExpectedTestException("Exception from ExecuteAsync's returned Task");
            }), cancellationToken);
        }
    }
}