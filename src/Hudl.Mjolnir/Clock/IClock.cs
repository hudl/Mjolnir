namespace Hudl.Mjolnir.Clock
{
    /// <summary>
    /// Abstracts system time, useful for mocking to manually control timing in classes like
    /// circuit breakers, which trip for periods of time and then automatically repair.
    /// </summary>
    internal interface IClock
    {
        long GetMillisecondTimestamp();
    }
}