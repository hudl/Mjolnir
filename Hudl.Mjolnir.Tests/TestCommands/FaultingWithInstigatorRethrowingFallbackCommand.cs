using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingWithInstigatorRethrowingFallbackCommand : FaultingWithoutFallbackCommand
    {
        protected override object Fallback(CommandFailedException instigator)
        {
            throw instigator;
        }
    }
}
