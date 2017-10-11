using System;

namespace Hudl.Mjolnir.Breaker
{
    /// <summary>
    /// Thrown when an operation is rejected by a circuit breaker.
    /// </summary>
    public class CircuitBreakerRejectedException : Exception {}
}
