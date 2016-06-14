using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Metrics;

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
                return new StandardCommandMetrics(
                    GroupKey.Named("Test"),
                    new TransientConfigurableValue<long>(30000),
                    new TransientConfigurableValue<long>(5000),
                    new IgnoringStats());
            }
        }

        public string Name
        {
            get { return "always-successful"; }
        }
    }
}