using System;
using System.Threading;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.Tests.Configuration.Helpers;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Configuration
{
    public class MjolnirConfigurationTests
    {
        [Fact]
        public void LoadConfig_LoadsSimpleProperty()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.Equal(true, mjolnirConfiguration.IsEnabled);
        }
        
        [Fact]
        public void LoadBulkheadDictionary_HasCorrectSize()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.Equal(mjolnirConfiguration.BulkheadConfigurations.Count, 1);
        }
        
        
        [Fact]
        public void LoadConfig_LoadsABulkheadDictionary()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.NotNull(mjolnirConfiguration.GetBulkheadConfiguration("TestGroupKey"));
        }       
        

        [Fact]
        public void LoadBulkheadDictionary_LoadsCorrectValue()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.Equal(5, mjolnirConfiguration.GetBulkheadConfiguration("TestGroupKey").MaxConcurrent);
        }
        
        [Fact]
        public void AfterConfigurationSubscription_OnConfigReloadWeAreBeingNotified()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider(TimeSpan.FromMilliseconds(50));
            var configObserverMock = new Mock<IObserver<MjolnirConfiguration>>();
            var onNextWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
            configObserverMock.Setup(o => o.OnNext(It.IsAny<MjolnirConfiguration>())).Callback(() =>
            {
                onNextWaitHandle.Set();
            });

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            mjolnirConfiguration.Subscribe(configObserverMock.Object);

            // Wait 5 seconds for update. If does not happen we have a problem. It shoul fire in ~50 milliseconds.
            var signalled = onNextWaitHandle.WaitOne(5000);

            // Assert
            Assert.True(signalled);
        }

        [Fact]
        public void TestJsonFile_DoesNotHaveTypos()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.Equal(true, mjolnirConfiguration.IsEnabled);
            Assert.Equal(true, mjolnirConfiguration.IgnoreTimeouts);
            Assert.Equal(true, mjolnirConfiguration.UseCircuitBreakers);
            Assert.Equal(1000, mjolnirConfiguration.DefaultCommandConfiguration.Timeout);
            Assert.Equal(1000, mjolnirConfiguration.DefaultCommandConfiguration.Timeout);
            Assert.Equal(1050, mjolnirConfiguration.CommandConfigurations["TestKey"].Timeout);
            Assert.Equal(1030, mjolnirConfiguration.CommandConfigurations["TestKey2"].Timeout);
            Assert.Equal(10, mjolnirConfiguration.DefaultBulkheadConfiguration.MaxConcurrent);
            Assert.Equal(5, mjolnirConfiguration.BulkheadConfigurations["TestGroupKey"].MaxConcurrent);
            Assert.Equal(1000, mjolnirConfiguration.DefaultBreakerConfiguration.WindowMillis);
            Assert.Equal(10, mjolnirConfiguration.DefaultBreakerConfiguration.MinimumOperations);
            Assert.Equal(50, mjolnirConfiguration.DefaultBreakerConfiguration.ThresholdPercentage);
            Assert.Equal(1000, mjolnirConfiguration.DefaultBreakerConfiguration.TrippedDurationMillis);
            Assert.Equal(false, mjolnirConfiguration.DefaultBreakerConfiguration.ForceTripped);
            Assert.Equal(false, mjolnirConfiguration.DefaultBreakerConfiguration.ForceFixed);
            Assert.Equal(1000, mjolnirConfiguration.BreakerConfigurations["TestKey"].WindowMillis);
            Assert.Equal(10, mjolnirConfiguration.BreakerConfigurations["TestKey"].MinimumOperations);
            Assert.Equal(50, mjolnirConfiguration.BreakerConfigurations["TestKey"].ThresholdPercentage);
            Assert.Equal(1000, mjolnirConfiguration.BreakerConfigurations["TestKey"].TrippedDurationMillis);
            Assert.Equal(false, mjolnirConfiguration.BreakerConfigurations["TestKey"].ForceTripped);
            Assert.Equal(false, mjolnirConfiguration.BreakerConfigurations["TestKey"].ForceFixed);
            Assert.Equal(500, mjolnirConfiguration.BreakerConfigurations["TestKey2"].WindowMillis);
            Assert.Equal(5, mjolnirConfiguration.BreakerConfigurations["TestKey2"].MinimumOperations);
            Assert.Equal(500, mjolnirConfiguration.BreakerConfigurations["TestKey2"].ThresholdPercentage);
            Assert.Equal(2000, mjolnirConfiguration.BreakerConfigurations["TestKey2"].TrippedDurationMillis);
            Assert.Equal(true, mjolnirConfiguration.BreakerConfigurations["TestKey2"].ForceTripped);
            Assert.Equal(true, mjolnirConfiguration.BreakerConfigurations["TestKey2"].ForceFixed);
        }
    }
}