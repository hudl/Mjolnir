using System;

namespace Hudl.Mjolnir.Config
{
    internal static class BulkheadConfigurationObserverCreationExtensions
    {
        internal static IDisposable OnConfigurationChanged<T>(this BulkheadConfiguration currentConfig, Func<BulkheadConfiguration, T> propertyToCheck, Action<T> onChangeAction)
        {
            var observer = new BulkheadConfigurationObserver<T>(currentConfig, propertyToCheck, onChangeAction);
            return currentConfig.Subscribe(observer);
        }
    }
}