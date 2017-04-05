namespace Hudl.Mjolnir.Metrics
{
    internal interface ICommandMetrics
    {
        //void IncrementConcurrent();
        //void DecrementConcurrent();

        MetricsSnapshot GetSnapshot();
        void Reset();
        
        // Could also track durations in the future if needed.
        void MarkCommandSuccess(/*TimeSpan duration*/);
        void MarkCommandFailure(/*TimeSpan duration*/);
        
        //void MarkExecutionTime(TimeSpan duration);
        //void MarkObservedExecutionTime(TimeSpan duration);
    }
}
