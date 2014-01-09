using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Throws a NotImplementedException from ExecuteAsync.
    /// </summary>
    internal class FaultingWithoutFallbackCommand : BaseTestCommand<object>
    {
        internal FaultingWithoutFallbackCommand() { }
        internal FaultingWithoutFallbackCommand(TimeSpan timeout) : base(timeout) { }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            throw new ExpectedTestException("Exception from ExecuteAsync");
        }
    }
}