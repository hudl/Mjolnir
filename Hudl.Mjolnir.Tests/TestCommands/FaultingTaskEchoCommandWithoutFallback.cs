using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingTaskEchoCommandWithoutFallback : BaseTestCommand<object>
    {
        private readonly Exception _exception;

        internal FaultingTaskEchoCommandWithoutFallback(Exception toRethrow)
        {
            _exception = toRethrow;
        }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(new Func<object>(() =>
            {
                throw _exception;
            }), cancellationToken);
        }
    }
}
