using Hudl.Mjolnir.Tests.Helper;
using Hudl.Mjolnir.Util;
using Xunit;

namespace Hudl.Mjolnir.Tests.Util
{
    public class NamingUtilTests : TestFixture
    {
        [Fact]
        public void GetLastAssemblyPart_WithDotSeparatedAssembly_ReturnsLastPart()
        {
            var currentType = GetType();

            Assert.Equal("Hudl.Mjolnir.Tests", currentType.Assembly.GetName().Name);
            Assert.Equal("Tests", NamingUtil.GetLastAssemblyPart(currentType));
        }

        [Fact]
        public void GetLastAssemblyPart_ForNameWithoutDots_ReturnsName()
        {
            // TODO How can we fake an assembly here? I tried mocking Type, but that was a no-go.
            // - We may just have to create a test project to use.
        }
    }
}
