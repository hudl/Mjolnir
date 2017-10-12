using Hudl.Mjolnir.Tests.Helper;
using System.Collections.Generic;
using Hudl.Mjolnir.Config;
using Xunit;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class BulkheadConfigTests : TestFixture
    {

        [Fact]
        public void GetMaxConcurrent_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var expectedConfigValue = AnyPositiveInt;

            var config = new TestConfiguration(bulkheadConfigurations: new Dictionary<string, BulkheadConfiguration>
            {
                {
                    groupKey.Name,
                    new BulkheadConfiguration
                    {
                        MaxConcurrent = expectedConfigValue
                    }
                }
            });

            // Act

            var value = config.GetBulkheadConfiguration(groupKey).MaxConcurrent;

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMaxConcurrent_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var expectedConfigValue = AnyPositiveInt;

            var config = new TestConfiguration(defaultBulkheadConfiguration: new BulkheadConfiguration
                {
                    MaxConcurrent = expectedConfigValue
                }
            );

            // Act

            var value = config.GetBulkheadConfiguration(groupKey).MaxConcurrent;

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMaxConcurrent_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10()
        {
            // Arrange

            const int expectedDefaultMaxConcurrent = 10;

            var groupKey = AnyGroupKey;

            var config = new TestConfiguration(defaultBulkheadConfiguration: new BulkheadConfiguration
                {
                    MaxConcurrent = expectedDefaultMaxConcurrent
                }
            );

            // Act

            var value = config.GetBulkheadConfiguration(groupKey).MaxConcurrent;

            // Assert

            Assert.Equal(expectedDefaultMaxConcurrent, value);
        }
    }
}
