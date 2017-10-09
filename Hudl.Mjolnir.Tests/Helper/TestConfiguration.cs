using System.Collections.Generic;
using Hudl.Mjolnir.Config;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class TestConfiguration : MjolnirConfiguration
    {
        public TestConfiguration(Dictionary<string, BreakerConfiguration> breakerConfigurations = null,
            BreakerConfiguration defaultBreakerConfiguration = null,
            Dictionary<string, BulkheadConfiguration> bulkheadConfigurations = null,
            BulkheadConfiguration defaultBulkheadConfiguration = null,
            Dictionary<string, CommandConfiguration> commandConfigurations = null,
            CommandConfiguration defaultCommandConfiguration = null,
            bool? isEnabled = null,
            bool? ignoreTimeouts = null,
            bool? useCircuitBreakers = null)
        {
            BreakerConfigurations = breakerConfigurations ?? BreakerConfigurations;
            DefaultBreakerConfiguration = defaultBreakerConfiguration ?? DefaultBreakerConfiguration;
            BulkheadConfigurations = bulkheadConfigurations ?? BulkheadConfigurations;
            DefaultBulkheadConfiguration = defaultBulkheadConfiguration ?? DefaultBulkheadConfiguration;
            CommandConfigurations = commandConfigurations ?? CommandConfigurations;
            DefaultCommandConfiguration = defaultCommandConfiguration ?? DefaultCommandConfiguration;
            IsEnabled = isEnabled ?? IsEnabled;
            IgnoreTimeouts = ignoreTimeouts ?? IgnoreTimeouts;
            UseCircuitBreakers = useCircuitBreakers ?? UseCircuitBreakers;
        }
    }
}