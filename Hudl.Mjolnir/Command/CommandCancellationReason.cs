namespace Hudl.Mjolnir.Command
{
    public enum CommandCancellationReason
    {
        /// <summary>
        /// The Command was cancelled by a cancellation token that was explicitly passed to the called method.
        /// </summary>
        CallerTokenCancellation,

        /// <summary>
        /// The Command was cancelled as a result of a preset timeout. 
        /// </summary>
        TimeoutCancellation
    }
}
