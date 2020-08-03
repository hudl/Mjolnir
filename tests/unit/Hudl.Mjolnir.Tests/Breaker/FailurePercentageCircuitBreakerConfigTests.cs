// using Hudl.Mjolnir.Breaker;
// using Hudl.Mjolnir.Tests.Helper;
// using System;
// using System.Collections.Generic;
// using Hudl.Mjolnir.Config;
// using Xunit;

// namespace Hudl.Mjolnir.Tests.Breaker
// {
//     public class FailurePercentageCircuitBreakerConfigTests : TestFixture
//     {
//         [Fact]
//         public void Constructor_ThrowsIfNullConfig()
//         {
//             // Arrange

//             // Act + Assaert

//             var exception = Assert.Throws<ArgumentNullException>(() => new FailurePercentageCircuitBreakerConfig(null));
//             Assert.Equal("config", exception.ParamName);
//         }

//         [Fact]
//         public void GetMinimumOperations_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             MinimumOperations = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetMinimumOperations(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetMinimumOperations_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     MinimumOperations = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetMinimumOperations(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetMinimumOperations_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10()
//         {
//             // Arrange

//             const long expectedDefaultMinimumOperations = 10;

//             var groupKey = AnyGroupKey;
//             const int expectedConfigValue = 10;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     MinimumOperations = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetMinimumOperations(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultMinimumOperations, value);
//         }

//         [Fact]
//         public void GetWindowMillis_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             WindowMillis = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetWindowMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetWindowMillis_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     WindowMillis = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);


//             // Act

//             var value = config.GetWindowMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetWindowMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs30000()
//         {
//             // Arrange

//             const long expectedDefaultWindowMillis = 30000;

//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     WindowMillis = expectedDefaultWindowMillis
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetWindowMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultWindowMillis, value);
//         }

//         [Fact]
//         public void GetThresholdPercentage_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             ThresholdPercentage = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetThresholdPercentage(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetThresholdPercentage_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ThresholdPercentage = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetThresholdPercentage(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetThresholdPercentage_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs50()
//         {
//             // Arrange

//             const int expectedDefaultThresholdPercentage = 50;

//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ThresholdPercentage = expectedDefaultThresholdPercentage
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetThresholdPercentage(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultThresholdPercentage, value);
//         }

//         [Fact]
//         public void GetTrippedDurationMillis_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             TrippedDurationMillis = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetTrippedDurationMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetTrippedDurationMillis_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     TrippedDurationMillis = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetTrippedDurationMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetTrippedDurationMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10000()
//         {
//             // Arrange

//             const long expectedDefaultTrippedDurationMillis = 10000;
//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     TrippedDurationMillis = expectedDefaultTrippedDurationMillis
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetTrippedDurationMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultTrippedDurationMillis, value);
//         }

//         [Fact]
//         public void GetForceTripped_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyBool;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             ForceTripped = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceTripped(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetForceTripped_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyBool;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ForceTripped = expectedConfigValue
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceTripped(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetForceTripped_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIsFalse()
//         {
//             // Arrange

//             const bool expectedDefaultForceTripped = false;

//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ForceTripped = expectedDefaultForceTripped
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceTripped(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultForceTripped, value);
//         }

//         [Fact]
//         public void GetForceFixed_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyBool;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             ForceFixed = expectedConfigValue
//                         }
//                     }
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceFixed(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetForceFixed_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyBool;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ForceFixed = expectedConfigValue
//                 }
//             };
//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceFixed(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetForceFixed_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIsFalse()
//         {
//             // Arrange

//             const bool expectedDefaultForceFixed = false;

//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     ForceFixed = expectedDefaultForceFixed
//                 }
//             };
//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetForceFixed(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultForceFixed, value);
//         }

//         [Fact]
//         public void GetSnapshotTtlMillis_UsesSpecificValueIfConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 BreakerConfigurations = new Dictionary<string, BreakerConfiguration>
//                 {
//                     {
//                         groupKey.Name,
//                         new BreakerConfiguration
//                         {
//                             SnapshotTtlMillis = expectedConfigValue
//                         }
//                     }
//                 }
//             };
//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetSnapshotTtlMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetSnapshotTtlMillis_UsesDefaultValueIfNoSpecificValueConfigured()
//         {
//             // Arrange

//             var groupKey = AnyGroupKey;
//             var expectedConfigValue = AnyPositiveInt;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     SnapshotTtlMillis = expectedConfigValue
//                 }
//             };
//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetSnapshotTtlMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedConfigValue, value);
//         }

//         [Fact]
//         public void GetSnapshotTtlMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs1000()
//         {
//             // Arrange

//             const long expectedDefaultSnapshotTtlMillis = 1000;

//             var groupKey = AnyGroupKey;

//             var mockConfig = new MjolnirConfiguration
//             {
//                 DefaultBreakerConfiguration = new BreakerConfiguration
//                 {
//                     SnapshotTtlMillis = expectedDefaultSnapshotTtlMillis
//                 }
//             };

//             var config = new FailurePercentageCircuitBreakerConfig(mockConfig);

//             // Act

//             var value = config.GetSnapshotTtlMillis(groupKey);

//             // Assert

//             Assert.Equal(expectedDefaultSnapshotTtlMillis, value);
//         }
//     }
// }