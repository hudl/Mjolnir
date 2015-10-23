using System;

namespace Hudl.Mjolnir.Command
{
    public sealed class CommandRejectedException : CommandFailedException
    {
        internal CommandRejectedException(Exception cause)
            : base("Command rejected", cause, CommandCompletionStatus.Rejected)
        {
        }
    }
}
