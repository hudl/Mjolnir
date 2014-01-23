using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hudl.Config;
using Moq;

namespace Hudl.Mjolnir.Tests.Helper
{
    public class TestFixture
    {
        public TestFixture()
        {
            UseTempConfig();
        }

        private static void UseTempConfig()
        {
            ConfigProvider.UseProvider(new FileConfigurationProvider(Path.Combine(Path.GetTempPath(), "hudl-test-config")));
            new ConfigurableValue<bool>("mjolnir.useCircuitBreakers").Value = true;

            //var mock = new Mock<IConfigurationProvider>();
            //mock.Setup(m => m.Get<bool>("mjolnir.useCircuitBreakers")).Returns(true);
            //mock.Setup(m => m.Get("mjolnir.useCircuitBreakers")).Returns(true);
            //ConfigProvider.UseProvider(mock.Object);
        }
    }
}
