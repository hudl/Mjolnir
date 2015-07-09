using System;
using Hudl.Config;
using Hudl.Mjolnir.Tests.Util;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixtureIgnoreTimeouts
    {
        public TestFixtureIgnoreTimeouts()
        {
            UseTempConfig();
        }

        private static void UseTempConfig()
        {
            var provider = new TestConfigProvider();
            provider.Set("mjolnir.useCircuitBreakers", true);
            provider.Set("mjolnir.ignoreTimeouts", true);

            ConfigProvider.UseProvider(new TestConfigProvider());
        }
    }
}
