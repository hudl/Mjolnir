using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingTaskWithSuccessfulFallbackCommand : FaultingTaskWithoutFallbackCommand
    {
        protected override object Fallback(CommandFailedException instigator)
        {
            return new { };
        }
    }
}