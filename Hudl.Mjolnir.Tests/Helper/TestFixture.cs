namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixture
    {
        public TestFixture()
        {
            ConfigProviderContext.Instance.SetConfigValue(ConfigProviderContext.UseCircuitBreakersKey, true);
            ConfigProviderContext.Instance.SetConfigValue(ConfigProviderContext.IgnoreTimeoutsKey, false);
        }
    }
}
