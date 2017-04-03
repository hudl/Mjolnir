using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;
using Moq;

namespace Hudl.Mjolnir.Tests.Helper
{
    /// <summary>
    /// Allows everything.
    /// </summary>
    internal class AlwaysSuccessfulCircuitBreaker : ICircuitBreaker
    {
        public bool IsAllowing()
        {
            return true;
        }

        public void MarkSuccess(long elapsedMillis)
        {
            // No-op.
        }

        public ICommandMetrics Metrics
        {
            get
            {
                var config = new DefaultValueConfig();
                var metricsConfig = new StandardCommandMetricsConfig(config);
                
                return new StandardCommandMetrics(GroupKey.Named("Test"), metricsConfig, new DefaultMjolnirLogFactory());
            }
        }

        public string Name
        {
            get { return "always-successful"; }
        }
    }
}