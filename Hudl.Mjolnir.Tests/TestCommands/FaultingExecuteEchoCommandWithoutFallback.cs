using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingExecuteEchoCommandWithoutFallback : BaseTestCommand<object>
    {
        private readonly Exception _exception;

        internal FaultingExecuteEchoCommandWithoutFallback(Exception toRethrow)
        {
            _exception = toRethrow;
        }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
