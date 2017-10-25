using Hudl.Mjolnir.External;

namespace Hudl.Mjolnir.Events
{
    /// <summary>
    /// Default implementation for IMetricEvents that ignores all method calls.
    /// </summary>
    internal sealed class IgnoringMetricEvents : IMetricEvents
    {
        public void BreakerGauge(string breakerName, long configuredMinimumOperations, long configuredWindowMillis, int configuredThresholdPercent, long configuredTrippedDurationMillis, bool configuredForceTripped, bool configuredForceFixed, bool isTripped, long windowSuccessCount, long windowFailureCount)
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

        public void BulkheadGauge(string bulkheadName, string bulkheadType, int maxConcurrent, int countAvailable)
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