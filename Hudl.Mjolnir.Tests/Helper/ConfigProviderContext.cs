using System;
using Hudl.Config;
using Hudl.Mjolnir.Tests.Util;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal sealed class ConfigProviderContext
    {
        private static readonly Lazy<ConfigProviderContext> _instance = new Lazy<ConfigProviderContext>(()=>new ConfigProviderContext());

        internal const string UseCircuitBreakersKey = "mjolnir.useCircuitBreakers";

        internal const string IgnoreTimeoutsKey = "mjolnir.ignoreTimeouts"; 

        internal static ConfigProviderContext Instance
        {
            get { return _instance.Value; }
        }

        private ConfigProviderContext()
        {
            var configProvider = new TestConfigProvider();
            ConfigProvider.UseProvider(configProvider);
        }

        internal void SetConfigValue(string key, object value)
        {
            ConfigProvider.Instance.Set(key, value);
        }
    }
}
