using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingTaskWithEchoThrowingFallbackCommand : FaultingTaskWithoutFallbackCommand
    {
        private readonly ExpectedTestException _exception;

        internal FaultingTaskWithEchoThrowingFallbackCommand(ExpectedTestException toRethrow)
        {
            _exception = toRethrow;
        }

        protected override object Fallback(CommandFailedException instigator)
        {
            throw _exception;
        }
    }
}