namespace Hudl.Mjolnir.External
{
    /// <summary>
    /// Clients can implement IMetricEvents to hook into metrics fired by Mjolnir components. The
    /// implementation can transform the metrics into the namespace and format of their choice,
    /// likely forwarding them onto a collector for aggregation and analysis.
    /// 
    /// Each method has a "recommended metric" in its doc comments. These are suggestions if you're
    /// using a "Coda Hale" style metrics collector like statsd or Metrics.NET.
    /// 
    /// TODO open-source our adapters/implementation(s)
    /// </summary>
    public interface IMetricEvents
    {
        /// <summary>
        /// When a command is invoked. Fires on completion.
        /// 
        /// Recommended metric: Timer, with fields like "status" and "failureAction" as dimensions.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="invokeMillis">
        ///     The elapsed time of the entire Invoke() call (microseconds). Subtracting the
        ///     <code>executeMicros</code> value yields the Mjolnir overhead during the call.
        /// </param>
        /// <param name="executeMillis">
        ///     The elapsed time of the Execute() call defined on the Command (microseconds).
        /// </param>
        /// <param name="status">
        ///     Subject to change, but is typically one of: success, rejected, failed, timeout.
        /// </param>
        /// <param name="failureAction">One of "throw" or "return".</param>
        void CommandInvoked(string commandName, double invokeMillis, double executeMillis, string status, string failureAction);

        /// <summary>
        /// When an operation acquires a lock/thread on its bulkhead.
        /// 
        /// Recommended metric: Counter (increment, opposite LeaveBulkhead).
        /// </summary>
        /// <param name="bulkheadName">Name of the bulkhead being entered.</param>
        /// <param name="commandName">Name of the command entering the bulkhead.</param>
        void EnterBulkhead(string bulkheadName, string commandName);

        /// <summary>
        /// When an operation releases the lock/thread it acquired on its bulkhead.
        /// 
        /// Recommended metric: Counter (decrement, opposite EnterBulkhead).
        /// </summary>
        /// <param name="bulkheadName">Name of the bulkhead being left.</param>
        /// <param name="commandName">Name of the command leaving the bulkhead.</param>
        void LeaveBulkhead(string bulkheadName, string commandName);

        /// <summary>
        /// When a bulkhead rejects a command because there's not enough capacity.
        /// 
        /// Recommended metric: Meter
        /// </summary>
        /// <param name="bulkheadName">Name of the bulkhead rejecting the command.</param>
        /// <param name="commandName">Name of the rejected command.</param>
        void RejectedByBulkhead(string bulkheadName, string commandName);

        /// <summary>
        /// Fires at (configurable) intervals, providing the current configuration state of the
        /// bulkhead. The default interval is 60 seconds.
        /// 
        /// Recommended metric: Gauge
        /// </summary>
        /// <param name="bulkheadName">The name of the bulkhead.</param>
        /// <param name="bulkheadType">The bulkhead type, e.g. "semaphore".</param>
        /// <param name="maxConcurrent">
        ///     The maximum concurrent ops the bulkhead currently allows.
        /// </param>
        void BulkheadConfigGauge(string bulkheadName, string bulkheadType, int maxConcurrent); // TODO wire up

        /// <summary>
        /// When a circuit breaker trips.
        /// </summary>
        /// <param name="breakerName">Name of the tripped breaker.</param>
        void BreakerTripped(string breakerName);

        /// <summary>
        /// When a circuit breaker becomes fixed, i.e. is no longer tripped.
        /// </summary>
        /// <param name="breakerName">Name of the fixed breaker.</param>
        void BreakerFixed(string breakerName);
        
        /// <summary>
        /// When an operation is rejected by the breaker because the breaker is tripped.
        /// 
        /// Recommended metric: Meter
        /// </summary>
        /// <param name="breakerName">Name of the breaker rejecting the command.</param>
        /// <param name="commandName">Name of the rejected command.</param>
        void RejectedByBreaker(string breakerName, string commandName);

        /// <summary>
        /// When an operation executes on the breaker successfully, contributing to the breaker's
        /// success count.
        /// 
        /// Recommended metric: Meter
        /// </summary>
        /// <param name="breakerName">Name of the breaker.</param>
        /// <param name="commandName">Name of the command executing on the breaker.</param>
        void BreakerSuccessCount(string breakerName, string commandName);

        /// <summary>
        /// When an operation executes on the breaker and fails, contributing to the breaker
        /// failure count. Note that rejections are not considered failures because they never
        /// "pass through" the breaker.
        /// 
        /// Recommended metric: Meter
        /// </summary>
        /// <param name="breakerName">Name of the breaker.</param>
        /// <param name="commandName">Name of the command executing on the breaker.</param>
        void BreakerFailureCount(string breakerName, string commandName);

        /// <summary>
        /// Fires at (configurable) intervals, providing the current configuration state of the
        /// breaker.
        /// 
        /// Recommended metric: Gauge
        /// </summary>
        /// <param name="breakerName">Name of the breaker.</param>
        /// <param name="minimumOps">
        ///     Minimum ops the breaker must see in its observation window before tripping.
        /// </param>
        /// <param name="windowMillis">
        ///     The window the breaker counts successes/failures within.
        /// </param>
        /// <param name="thresholdPercent">
        ///     Error rate in the observation window required to trip.
        /// </param>
        /// <param name="tripForMillis">
        ///     Duration the breaker remains tripped for before testing.
        /// </param>
        void BreakerConfigGauge(string breakerName, long minimumOps, long windowMillis, int thresholdPercent, long tripForMillis); // TODO wire up
    }

    internal sealed class IgnoringMetricEvents : IMetricEvents
    {
        public void BreakerConfigGauge(string breakerName, long windowMillis, long minimumOps, int thresholdPercent, long tripForMillis)
        {
            return;
        }

        public void BreakerFailureCount(string breakerName, string commandName)
        {
            return;
        }

        public void BreakerFixed(string breakerName)
        {
            return;
        }

        public void BreakerSuccessCount(string breakerName, string commandName)
        {
            return;
        }

        public void BreakerTripped(string breakerName)
        {
            return;
        }

        public void BulkheadConfigGauge(string bulkheadName, string bulkheadType, int maxConcurrent)
        {
            return;
        }

        public void CommandInvoked(string commandName, double invokeMillis, double executeMillis, string status, string failureMode)
        {
            return;
        }

        public void EnterBulkhead(string bulkheadName, string commandName)
        {
            return;
        }

        public void LeaveBulkhead(string bulkheadName, string commandName)
        {
            return;
        }

        public void RejectedByBreaker(string breakerName, string commandName)
        {
            return;
        }

        public void RejectedByBulkhead(string bulkheadName, string commandName)
        {
            return;
        }
    }
}
