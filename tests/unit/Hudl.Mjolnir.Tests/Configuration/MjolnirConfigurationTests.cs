using System;
using System.Threading;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.Key;
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
            Assert.NotNull(mjolnirConfiguration.GetBulkheadConfiguration(GroupKey.Named("testGroupKey")));
        }       
        

        [Fact]
        public void LoadBulkheadDictionary_LoadsCorrectValue()
        {
            // Arrange
            var configProvider = new ExampleJsonConfigProvider();

            // Act
            var mjolnirConfiguration = configProvider.GetConfig();
            
            // Assert
            Assert.Equal(5, mjolnirConfiguration.GetBulkheadConfiguration(GroupKey.Named("testGroupKey")).MaxConcurrent);
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
    }
}