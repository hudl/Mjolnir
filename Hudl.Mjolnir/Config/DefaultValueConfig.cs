using System.Collections.Generic;

namespace Hudl.Mjolnir.Config
{
    /// <summary>
    /// Default implementation for config that returns the default value passed into GetConfig().
    /// This is used as a fallback implementation. Most consumers will probably want to wire in 
    /// their own configuration implementation.
    /// </summary>
    internal class DefaultValueConfig : MjolnirConfiguration
    {
        public DefaultValueConfig()
        {
            IsEnabled = true;
            UseCircuitBreakers = true;
            IgnoreTimeouts = false;
        }

    }
}