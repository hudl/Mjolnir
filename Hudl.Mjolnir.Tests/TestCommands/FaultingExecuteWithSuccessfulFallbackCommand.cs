using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingExecuteWithSuccessfulFallbackCommand : FaultingExecuteWithoutFallbackCommand
    {
        protected override object Fallback(CommandFailedException instigator)
        {
            return new { };
        }
    }
}
