using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using System.Collections.Generic;
using Hudl.Mjolnir.Config;
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
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new TestConfiguration(bulkheadConfigurations: new Dictionary<string, BulkheadConfiguration>
            {
                {
                    groupKey.Name,
                    new BulkheadConfiguration
                    {
                        MaxConcurrent = expectedConfigValue
                    }
                }
            });

            var config = new BulkheadConfig(mockConfig);

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
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new TestConfiguration(defaultBulkheadConfiguration: new BulkheadConfiguration
                {
                    MaxConcurrent = expectedConfigValue
                }
            );

            var config = new BulkheadConfig(mockConfig);

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

            var mockConfig = new TestConfiguration(defaultBulkheadConfiguration: new BulkheadConfiguration
                {
                    MaxConcurrent = expectedDefaultMaxConcurrent
                }
            );
            var config = new BulkheadConfig(mockConfig);

            // Act

            var value = config.GetMaxConcurrent(groupKey);

            // Assert

            Assert.Equal(expectedDefaultMaxConcurrent, value);
        }
    }
}
