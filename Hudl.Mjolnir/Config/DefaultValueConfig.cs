using Hudl.Mjolnir.External;
using System;

namespace Hudl.Mjolnir.Config
{
    /// <summary>
    /// Default implementation for config that returns the default value passed into GetConfig().
    /// This is used as a fallback implementation. Most consumers will probably want to wire in 
    /// their own configuration implementation.
    /// </summary>
    internal class DefaultValueConfig : IMjolnirConfig
    {
        public T GetConfig<T>(string key, T defaultValue)
        {
            return defaultValue;
        }

        public void AddChangeHandler<T>(string key, Action<T> onConfigChange)
        {
            // No-op for default value config.
        }
    }
}
