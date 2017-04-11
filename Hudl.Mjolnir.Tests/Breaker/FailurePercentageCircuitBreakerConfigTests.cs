using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using System;
using Xunit;

namespace Hudl.Mjolnir.Tests.Breaker
{
    public class FailurePercentageCircuitBreakerConfigTests : TestFixture
    {
        [Fact]
        public void Constructor_ThrowsIfNullConfig()
        {
            // Arrange

            // Act + Assert

            var exception = Assert.Throws<ArgumentNullException>(() => new FailurePercentageCircuitBreakerConfig(null));
            Assert.Equal("config", exception.ParamName);
        }

        [Fact]
        public void GetMinimumOperations_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetMinimumOperations(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMinimumOperations_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            const string defaultConfigKey = "mjolnir.breaker.default.minimumOperations";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<long>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetMinimumOperations(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMinimumOperations_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10()
        {
            // Arrange

            const long expectedDefaultMinimumOperations = 10;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            const string defaultConfigKey = "mjolnir.breaker.default.minimumOperations";
            

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultMinimumOperations)).Returns(expectedDefaultMinimumOperations);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetMinimumOperations(groupKey);

            // Assert

            Assert.Equal(expectedDefaultMinimumOperations, value);
        }

        [Fact]
        public void GetWindowMillis_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.windowMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetWindowMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetWindowMillis_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.windowMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.windowMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<long>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetWindowMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetWindowMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs30000()
        {
            // Arrange

            const long expectedDefaultWindowMillis = 30000;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.windowMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.windowMillis";


            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultWindowMillis)).Returns(expectedDefaultWindowMillis);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetWindowMillis(groupKey);

            // Assert

            Assert.Equal(expectedDefaultWindowMillis, value);
        }

        [Fact]
        public void GetThresholdPercentage_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetThresholdPercentage(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetThresholdPercentage_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            const string defaultConfigKey = "mjolnir.breaker.default.thresholdPercentage";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns((int?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<int>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetThresholdPercentage(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetThresholdPercentage_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs50()
        {
            // Arrange

            const int expectedDefaultThresholdPercentage = 50;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            const string defaultConfigKey = "mjolnir.breaker.default.thresholdPercentage";
            
            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns((int?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultThresholdPercentage)).Returns(expectedDefaultThresholdPercentage);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetThresholdPercentage(groupKey);

            // Assert

            Assert.Equal(expectedDefaultThresholdPercentage, value);
        }

        [Fact]
        public void GetTrippedDurationMillis_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetTrippedDurationMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetTrippedDurationMillis_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.trippedDurationMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<long>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetTrippedDurationMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetTrippedDurationMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs10000()
        {
            // Arrange

            const long expectedDefaultTrippedDurationMillis = 10000;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.trippedDurationMillis";
            
            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultTrippedDurationMillis)).Returns(expectedDefaultTrippedDurationMillis);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetTrippedDurationMillis(groupKey);

            // Assert

            Assert.Equal(expectedDefaultTrippedDurationMillis, value);
        }

        [Fact]
        public void GetForceTripped_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceTripped";
            var expectedConfigValue = AnyBool;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceTripped(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceTripped_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceTripped";
            const string defaultConfigKey = "mjolnir.breaker.default.forceTripped";
            var expectedConfigValue = AnyBool;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<bool>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceTripped(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceTripped_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIsFalse()
        {
            // Arrange

            const bool expectedDefaultForceTripped = false;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceTripped";
            const string defaultConfigKey = "mjolnir.breaker.default.forceTripped";
            

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultForceTripped)).Returns(expectedDefaultForceTripped);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceTripped(groupKey);

            // Assert

            Assert.Equal(expectedDefaultForceTripped, value);
        }

        [Fact]
        public void GetForceFixed_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceFixed";
            var expectedConfigValue = AnyBool;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceFixed(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceFixed_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceFixed";
            const string defaultConfigKey = "mjolnir.breaker.default.forceFixed";
            var expectedConfigValue = AnyBool;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<bool>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceFixed(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceFixed_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIsFalse()
        {
            // Arrange

            const bool expectedDefaultForceFixed = false;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceFixed";
            const string defaultConfigKey = "mjolnir.breaker.default.forceFixed";
            
            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultForceFixed)).Returns(expectedDefaultForceFixed);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceFixed(groupKey);

            // Assert

            Assert.Equal(expectedDefaultForceFixed, value);
        }

        [Fact]
        public void GetSnapshotTtlMillis_UsesSpecificValueIfConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.snapshotTtlMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetSnapshotTtlMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetSnapshotTtlMillis_UsesDefaultValueIfNoSpecificValueConfigured()
        {
            // Arrange

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.snapshotTtlMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.snapshotTtlMillis";
            var expectedConfigValue = AnyPositiveInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, It.IsAny<long>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetSnapshotTtlMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetSnapshotTtlMillis_UsesDefaultValueIfNoSpecificValueOrDefaultValueConfigured_DefaultIs1000()
        {
            // Arrange

            const long expectedDefaultSnapshotTtlMillis = 1000;

            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.snapshotTtlMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.snapshotTtlMillis";


            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedDefaultSnapshotTtlMillis)).Returns(expectedDefaultSnapshotTtlMillis);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetSnapshotTtlMillis(groupKey);

            // Assert

            Assert.Equal(expectedDefaultSnapshotTtlMillis, value);
        }
    }
}
