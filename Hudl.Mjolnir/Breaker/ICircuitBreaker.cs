using Hudl.Mjolnir.Metrics;

namespace Hudl.Mjolnir.Breaker
{
    internal interface ICircuitBreaker
    {
        bool IsAllowing();
        void MarkSuccess(long elapsedMillis);

        ICommandMetrics Metrics { get; }
        string Name { get; }
    }
}