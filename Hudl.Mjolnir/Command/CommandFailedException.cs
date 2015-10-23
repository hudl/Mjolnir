using System;

namespace Hudl.Mjolnir.Command
{
    public class CommandFailedException : Exception
    {
        internal bool IsFallbackImplemented { get; set; }
        public CommandCompletionStatus Status { get; internal set; }
        public FallbackStatus FallbackStatus { get; internal set; }

        internal CommandFailedException(Exception cause) : this("Command failed", cause, CommandCompletionStatus.Faulted) { }

        protected CommandFailedException(string message, Exception cause, CommandCompletionStatus status) : base(message, cause)
        {
            IsFallbackImplemented = true; // Assume the best! Actually, we'll just set it to false later if we don't have an implementation.
            Status = status;
        }
    }
}