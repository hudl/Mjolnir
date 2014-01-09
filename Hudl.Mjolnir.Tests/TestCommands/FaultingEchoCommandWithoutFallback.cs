using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingEchoCommandWithoutFallback : BaseTestCommand<object>
    {
        private readonly Exception _exception;

        internal FaultingEchoCommandWithoutFallback(Exception toRethrow)
        {
            _exception = toRethrow;
        }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
