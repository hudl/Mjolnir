using System.Collections.Concurrent;
using Hudl.Mjolnir.External;

namespace Hudl.Mjolnir.Events
{
    public class GaugeLogMetrics : IMetricEvents
    {
        private IMjolnirLog<GaugeLogMetrics> _diagnosticLog;
        private IMjolnirLog<GaugeLogMetrics> _concurrencyExceededLog;
        private IMjolnirLog<GaugeLogMetrics> _breakerTrippedLog;
        private IMjolnirLog<GaugeLogMetrics> _bulkheadGaugeLog;
        private IMjolnirLog<GaugeLogMetrics> _breakerGaugeLog;
        private ConcurrentDictionary<string, int> _currentBulkheadsRejected;
        public GaugeLogMetrics(IMjolnirLogFactory logFactory)
        {
            _diagnosticLog = logFactory.CreateLog<GaugeLogMetrics>();
            _diagnosticLog.SetLogName($"{nameof(GaugeLogMetrics)}.Diagnostic");
            _concurrencyExceededLog = logFactory.CreateLog<GaugeLogMetrics>();
            _concurrencyExceededLog.SetLogName($"{nameof(GaugeLogMetrics)}.BulkheadConcurrencyExceeded");
            _breakerTrippedLog = logFactory.CreateLog<GaugeLogMetrics>();
            _breakerTrippedLog.SetLogName($"{nameof(GaugeLogMetrics)}.BreakerTripped");
            _bulkheadGaugeLog = logFactory.CreateLog<GaugeLogMetrics>();
            _bulkheadGaugeLog.SetLogName($"{nameof(GaugeLogMetrics)}.BulkheadGauge");
            _breakerGaugeLog = logFactory.CreateLog<GaugeLogMetrics>();
            _breakerGaugeLog.SetLogName($"{nameof(GaugeLogMetrics)}.BreakerGauge");
            _currentBulkheadsRejected = new ConcurrentDictionary<string, int>();
        }

        public void BreakerFailureCount(string breakerName, string commandName)
        {
            _diagnosticLog.Debug($"BreakerFailureCount - [Breaker={breakerName}, Command={commandName}]");
        }

        public void BreakerFixed(string breakerName)
        {
            var log = $"BreakerFixed - [Breaker={breakerName}]";
            _diagnosticLog.Debug(log);
            _breakerTrippedLog.Debug(log);
        }

        public void BreakerGauge(string breakerName, long configuredMinimumOperations, long configuredWindowMillis, int configuredThresholdPercent, long configuredTrippedDurationMillis, bool configuredForceTripped, bool configuredForceFixed, bool isTripped, long windowSuccessCount, long windowFailureCount)
        {
            var log = $"BreakerGauge - [Breaker={breakerName}, ConfiguredMinimumOperations={configuredMinimumOperations}, ConfiguredWindowMs={configuredWindowMillis}, ConfiguredThresholdPercent={configuredThresholdPercent}, ConfiguredTrippedDurationMs={configuredTrippedDurationMillis}, ConfiguredForceTripped={configuredForceTripped}, ConfiguredForcedFixed={configuredForceFixed}, IsTripped={isTripped}, WindowSuccessCount={windowSuccessCount}, WindowFailureCount={windowFailureCount}]";
            _diagnosticLog.Debug(log);
            _breakerGaugeLog.Debug(log);
            if (isTripped)
            {
                _breakerTrippedLog.Debug(log);
            }
        }

        public void BreakerSuccessCount(string breakerName, string commandName)
        {
            _diagnosticLog.Debug($"BreakerSuccessCount - [Breaker={breakerName}, Command={commandName}]");
        }

        public void BreakerTripped(string breakerName)
        {
            var log = $"BreakerTripped - [Breaker={breakerName}]";
            _diagnosticLog.Debug(log);
            _breakerTrippedLog.Debug(log);
        }

        public void BulkheadGauge(string bulkheadName, string bulkheadType, int maxConcurrent, int countAvailable)
        {
            var gaugeLog = $"BulkheadGauge - [Bulkhead={bulkheadName}, BulkheadType={bulkheadType}, MaxConcurrent={maxConcurrent}, CountAvailable={countAvailable}]";
            _diagnosticLog.Debug(gaugeLog);
            _bulkheadGaugeLog.Debug(gaugeLog);
            if (countAvailable == 0)
            {
                _concurrencyExceededLog.Debug(gaugeLog);
            }
            // Log the current rejections that occurred since the last gauge
            if (_currentBulkheadsRejected.TryGetValue(bulkheadName, out int currentCount))
            {
                if (currentCount > 0)
                {
                    _concurrencyExceededLog.Debug($"BulkheadRejections since last gauge - [Bulkhead={bulkheadName}, BulkheadType={bulkheadType}, MaxConcurrent={maxConcurrent}, CountAvailable={countAvailable}, Rejections={currentCount}]");
                    // Remove the rejections we've just logged from the current rejection count
                    _currentBulkheadsRejected.AddOrUpdate(bulkheadName, 0, (b, c) => c - currentCount);
                }
            }
        }

        public void CommandInvoked(string commandName, double invokeMillis, double executeMillis, string status, string failureAction)
        {
            _diagnosticLog.Debug($"CommandInvoked - [Command={commandName}, InvokeMs={invokeMillis}, ExecuteMs={executeMillis}, FailureAction={failureAction}]");
        }

        public void EnterBulkhead(string bulkheadName, string commandName)
        {
            _diagnosticLog.Debug($"EnterBulkhead - [Bulkhead={bulkheadName}, Command={commandName}]");
        }

        public void LeaveBulkhead(string bulkheadName, string commandName)
        {
            _diagnosticLog.Debug($"LeaveBulkhead - [Bulkhead={bulkheadName}, Command={commandName}]");
        }

        public void RejectedByBreaker(string breakerName, string commandName)
        {
            var log = $"RejectedByBreaker - [Breaker={breakerName}, Command={commandName}]";
            _diagnosticLog.Debug(log);
            _breakerTrippedLog.Debug(log);
        }

        public void RejectedByBulkhead(string bulkheadName, string commandName)
        {
            var log = $"RejectedByBulkhead - [Bulkhead={bulkheadName}, Command={commandName}]";
            _diagnosticLog.Debug(log);
            _concurrencyExceededLog.Debug(log);
            _currentBulkheadsRejected.AddOrUpdate(bulkheadName, 1, (bh, current) => current++);
        }
    }
}