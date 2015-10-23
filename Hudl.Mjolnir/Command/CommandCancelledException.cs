using System;

namespace Hudl.Mjolnir.Command
{
    public sealed class CommandCancelledException : CommandFailedException
    {
        internal CommandCancelledException(Exception cause)
            : base("Command canceled", cause, CommandCompletionStatus.Canceled)
        {
        }
    }
}
