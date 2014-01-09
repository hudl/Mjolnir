using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class FaultingWithSuccessfulFallbackCommand : FaultingWithoutFallbackCommand
    {
        protected override object Fallback(CommandFailedException instigator)
        {
            return new { };
        }
    }
}