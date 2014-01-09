namespace Hudl.Mjolnir.Command
{
    public enum CommandCompletionStatus
    {
        /// <summary>
        /// Finished successfully.
        /// </summary>
        RanToCompletion,

        /// <summary>
        /// Unrecognized error occurred.
        /// </summary>
        Faulted,

        /// <summary>
        /// Canceled (timed out).
        /// </summary>
        Canceled,

        /// <summary>
        /// Rejected by the circuit breaker.
        /// </summary>
        Rejected,
    }
}
