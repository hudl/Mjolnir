using System;

namespace Hudl.Mjolnir.Config
{
    internal static class MjolnirConfigurationExtensions
    {
        internal static IDisposable OnConfigurationChanged<T>(this MjolnirConfiguration currentConfig, Func<MjolnirConfiguration, T> propertyToCheck, Action<T> onChangeAction)
        {
            var observer = new MjolnirConfigurationObserver<T>(currentConfig, propertyToCheck, onChangeAction);
            return currentConfig.Subscribe(observer);
        }
    }
}