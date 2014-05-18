using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Throws a NotImplementedException from the Task returned by ExecuteAsync.
    /// </summary>
    internal class FaultingExecuteWithoutFallbackCommand : BaseTestCommand<object>
    {
        internal FaultingExecuteWithoutFallbackCommand() { }
        internal FaultingExecuteWithoutFallbackCommand(TimeSpan timeout) : base(timeout) { }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new ExpectedTestException("Exception from ExecuteAsync directly (not from returned Task)");
        }
    }
}