using System;
using System.Collections.Generic;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.Key;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class TestConfiguration : MjolnirConfiguration
    {
        private class Subscription: IDisposable
        {
            private readonly Action _onDispose;
            public Subscription(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }
        
        public TestConfiguration(Dictionary<string, BreakerConfiguration> breakerConfigurations = null,
            BreakerConfiguration defaultBreakerConfiguration = null,
            Dictionary<string, BulkheadConfiguration> bulkheadConfigurations = null,
            BulkheadConfiguration defaultBulkheadConfiguration = null,
            Dictionary<string, CommandConfiguration> commandConfigurations = null,
            CommandConfiguration defaultCommandConfiguration = null,
            bool? isEnabled = null,
            bool? ignoreTimeouts = null,
            bool? useCircuitBreakers = null)
        {
            BreakerConfigurations = breakerConfigurations ?? BreakerConfigurations;
            DefaultBreakerConfiguration = defaultBreakerConfiguration ?? DefaultBreakerConfiguration;
            BulkheadConfigurations = bulkheadConfigurations ?? BulkheadConfigurations;
            DefaultBulkheadConfiguration = defaultBulkheadConfiguration ?? DefaultBulkheadConfiguration;
            CommandConfigurations = commandConfigurations ?? CommandConfigurations;
            DefaultCommandConfiguration = defaultCommandConfiguration ?? DefaultCommandConfiguration;
            IsEnabled = isEnabled ?? IsEnabled;
            IgnoreTimeouts = ignoreTimeouts ?? IgnoreTimeouts;
            UseCircuitBreakers = useCircuitBreakers ?? UseCircuitBreakers;
            
            Observers = new List<IObserver<MjolnirConfiguration>>();

        }
        
        // Internal for tests purposes
        internal readonly List<IObserver<MjolnirConfiguration>> Observers;


        public void UpdateBulkheadConfiguration(GroupKey key, BulkheadConfiguration bulkheadConfiguration)
        {
            BulkheadConfigurations[key.Name] = bulkheadConfiguration;
            Observers?.ForEach(o => o.OnNext(this));
        }

        public override IDisposable Subscribe(IObserver<MjolnirConfiguration> observer)
        {
            var subscription = new Subscription(() => Observers.Remove(observer));
            Observers.Add(observer);
            return subscription;
        }
    }
}