namespace Hudl.Mjolnir.Metrics
{
    internal interface ICommandMetrics
    {
        //void IncrementConcurrent();
        //void DecrementConcurrent();

        MetricsSnapshot GetSnapshot();
        void Reset();

        void MarkCommandSuccess(/*TimeSpan duration*/); // TODO rob.hruska 11/8/2013 - Durations?
        void MarkCommandFailure(/*TimeSpan duration*/);
        
        //void MarkExecutionTime(TimeSpan duration);
        //void MarkObservedExecutionTime(TimeSpan duration);
    }
}
