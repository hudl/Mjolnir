using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using Xunit;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class BulkheadConfigTests : TestFixture
    {
        [Fact]
        public void Constructor_ThrowsIfNullConfig()
        {
            // Arrange

            // Act + Assert

            var exception = Assert.Throws<ArgumentNullException>(() => new BulkheadConfig(null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void GetMaxConcurrent_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.bulkhead.{groupKey}.maxConcurrent";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns(expectedConfigValue);

            var config = new BulkheadConfig(mockConfig.Object);

            // Act

            var value = config.GetMaxConcurrent(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMaxConcurrent_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.bulkhead.{groupKey}.maxConcurrent";
            const string defaultConfigKey = "mjolnir.bulkhead.default.maxConcurrent";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns((int?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<int>())).Returns(expectedConfigValue);

            var config = new BulkheadConfig(mockConfig.Object);

            // Act

            var value = config.GetMaxConcurrent(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMaxConcurrent_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10()
        {
            // Arrange

            const int expectedDefaultMaxConcurrent = 10;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.bulkhead.{groupKey}.maxConcurrent";
            const string defaultConfigKey = "mjolnir.bulkhead.default.maxConcurrent";

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns((int?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultMaxConcurrent)).Returns(expectedDefaultMaxConcurrent);

            var config = new BulkheadConfig(mockConfig.Object);

            // Act

            var value = config.GetMaxConcurrent(groupKey);

            // Assert

            Assert.Equal(expectedDefaultMaxConcurrent, value);
        }
    }
}
