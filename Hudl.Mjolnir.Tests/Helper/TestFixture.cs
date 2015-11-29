using Hudl.Config;
using Hudl.Mjolnir.Tests.Util;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixture
    {
        private static readonly TestConfigProvider _configProvider = new TestConfigProvider();
        private static bool _configured = false;

        internal const string IgnoreTimeoutsKey = "mjolnir.ignoreTimeouts"; 

        protected string AnyString
        {
            get { return Rand.String(); }
        }
        
        public TestFixture()
        {
            if (_configured)return;
            ConfigProvider.UseProvider(_configProvider);
            _configured = true;
        }
    }
}
