using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Breaker
{
    internal interface ICircuitBreakerFactory
    {
        ICircuitBreaker GetCircuitBreaker(GroupKey key);
    }
}
