using Hudl.Mjolnir.Metrics;

namespace Hudl.Mjolnir.Breaker
{
    internal interface ICircuitBreaker
    {
        /// <summary>
        /// Call this when you're attempting to pass something through the breaker. IsAllowing()
        /// will both check to see if a single request should be allowed, and if the breaker is not
        /// tripped/fixed but should be, will change the breaker state.
        /// </summary>
        bool IsAllowing();
        void MarkSuccess(long elapsedMillis);

        ICommandMetrics Metrics { get; }
        string Name { get; }
    }
}