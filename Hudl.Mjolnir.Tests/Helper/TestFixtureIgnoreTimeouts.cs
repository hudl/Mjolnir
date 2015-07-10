namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixtureIgnoreTimeouts
    {
        // Need to make sure that the config values are set to their initial state before each test.
        public TestFixtureIgnoreTimeouts()
        {
            ConfigProviderContext.Instance.SetConfigValue(ConfigProviderContext.UseCircuitBreakersKey, true);
            ConfigProviderContext.Instance.SetConfigValue(ConfigProviderContext.IgnoreTimeoutsKey, true);
        }
    }
}
