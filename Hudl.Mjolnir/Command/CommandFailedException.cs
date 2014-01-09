using System;

namespace Hudl.Mjolnir.Command
{
    public class CommandFailedException : Exception
    {
        internal bool IsFallbackImplemented { get; set; }
        public CommandCompletionStatus Status { get; internal set; }
        public FallbackStatus FallbackStatus { get; internal set; }

        internal CommandFailedException(Exception cause, CommandCompletionStatus status) : base(GetMessage(status), cause)
        {
            IsFallbackImplemented = true; // Assume the best! Actually, we'll just set it to false later if we don't have an implementation.
            Status = status;
        }

        private static string GetMessage(CommandCompletionStatus status)
        {
            switch (status)
            {
                case CommandCompletionStatus.Canceled:
                    return "Command canceled";

                case CommandCompletionStatus.Rejected:
                    return "Command rejected";
            }

            return "Command failed";
        }
    }
}