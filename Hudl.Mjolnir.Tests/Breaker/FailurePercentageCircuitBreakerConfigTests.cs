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
        public void GetMinimumOperations_UsesBreakerValueIfConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            var expectedConfigValue = AnyLong;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetMinimumOperations(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetMinimumOperations_UsesDefaultValueIfNoBreakerValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            const string defaultConfigKey = "mjolnir.breaker.default.minimumOperations";
            var expectedConfigValue = AnyLong;

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
        public void GetMinimumOperations_UsesDefaultValueIfNoBreakerValueOrDefaultValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.minimumOperations";
            const string defaultConfigKey = "mjolnir.breaker.default.minimumOperations";
            const long expectedConfigValue = 10; // Default hard-coded fallback.

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedConfigValue)).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetMinimumOperations(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetThresholdPercentage_UsesBreakerValueIfConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            var expectedConfigValue = AnyInt;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetThresholdPercentage(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetThresholdPercentage_UsesDefaultValueIfNoBreakerValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            const string defaultConfigKey = "mjolnir.breaker.default.thresholdPercentage";
            var expectedConfigValue = AnyInt;

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
        public void GetThresholdPercentage_UsesDefaultValueIfNoBreakerValueOrDefaultValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.thresholdPercentage";
            const string defaultConfigKey = "mjolnir.breaker.default.thresholdPercentage";
            const int expectedConfigValue = 50; // Default hard-coded fallback.

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<int?>())).Returns((int?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedConfigValue)).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetThresholdPercentage(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetTrippedDurationMillis_UsesBreakerValueIfConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            var expectedConfigValue = AnyLong;

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetTrippedDurationMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetTrippedDurationMillis_UsesDefaultValueIfNoBreakerValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.trippedDurationMillis";
            var expectedConfigValue = AnyLong;

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
        public void GetTrippedDurationMillis_UsesDefaultValueIfNoBreakerValueOrDefaultValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.trippedDurationMillis";
            const string defaultConfigKey = "mjolnir.breaker.default.trippedDurationMillis";
            const long expectedConfigValue = 10000; // Default hard-coded fallback.

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<long?>())).Returns((long?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedConfigValue)).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetTrippedDurationMillis(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceTripped_UsesBreakerValueIfConfigured()
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
        public void GetForceTripped_UsesDefaultValueIfNoBreakerValueConfigured()
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
        public void GetForceTripped_UsesDefaultValueIfNoBreakerValueOrDefaultValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceTripped";
            const string defaultConfigKey = "mjolnir.breaker.default.forceTripped";
            const bool expectedConfigValue = false; // Default hard-coded fallback.

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedConfigValue)).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceTripped(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }

        [Fact]
        public void GetForceFixed_UsesBreakerValueIfConfigured()
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
        public void GetForceFixed_UsesDefaultValueIfNoBreakerValueConfigured()
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
        public void GetForceFixed_UsesDefaultValueIfNoBreakerValueOrDefaultValueConfigured()
        {
            // Arrange
            var groupKey = AnyGroupKey;
            var specificConfigKey = $"mjolnir.breaker.{groupKey}.forceFixed";
            const string defaultConfigKey = "mjolnir.breaker.default.forceFixed";
            const bool expectedConfigValue = false; // Default hard-coded fallback.

            var mockConfig = new Mock<IMjolnirConfig>(MockBehavior.Strict);
            mockConfig.Setup(m => m.GetConfig(specificConfigKey, It.IsAny<bool?>())).Returns((bool?)null);
            mockConfig.Setup(m => m.GetConfig(defaultConfigKey, expectedConfigValue)).Returns(expectedConfigValue);

            var config = new FailurePercentageCircuitBreakerConfig(mockConfig.Object);

            // Act

            var value = config.GetForceFixed(groupKey);

            // Assert

            Assert.Equal(expectedConfigValue, value);
        }
    }
}
