using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingTaskWithInstigatorRethrowingFallbackCommand : FaultingTaskWithoutFallbackCommand
    {
        protected override object Fallback(CommandFailedException instigator)
        {
            throw instigator;
        }
    }
}
