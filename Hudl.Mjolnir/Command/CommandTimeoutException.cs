using System;

namespace Hudl.Mjolnir.Command
{
    public sealed class CommandTimeoutException : CommandFailedException
    {
        internal CommandTimeoutException(Exception cause)
            : base("Command timed out", cause, CommandCompletionStatus.TimedOut)
        {
        }
    }
}
