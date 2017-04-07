using System;

namespace Hudl.Mjolnir.External
{
    /// <summary>
    /// Implement this and pass it to CommandInvoker to inject a configuration provider.
    /// 
    /// If not implemented and injected, hard-coded system defaults will be used.
    /// </summary>
    public interface IMjolnirConfig
    {
        /// <summary>
        /// Returns a config value for the provided key.
        /// 
        /// If a configuration value is not set for the provided key, the implementation should
        /// return defaultValue.
        /// 
        /// Implementations should handle non-nullable (e.g. primitive) types well. If Type T is an
        /// <code>int</code> and the configuration value is not set, the implementation should
        /// return defaultValue instead of <code>default(int)</code> (i.e. 0).
        /// 
        /// If Type T is a nullable primitive (e.g. <code>int?</code>) and the configuration value
        /// is not set, the implementation should honor <code>null</code> as a defaultValue and
        /// return it instead of <code>default(int)</code> (i.e. 0).
        /// 
        /// It's recommended that the implementation cache configured values by key if accessing
        /// configuration is expensive. Mjolnir uses dynamically-generated keys for breakers and
        /// bulkheads (e.g. mjolnir.bulkhead.my-bulkhead-key.maxConcurrent). The implementation
        /// should accommodate this behavior.
        /// 
        /// If the configured value cannot be cast to Type T, the implementation should throw an
        /// InvalidOperationException with detailed information about the cast failure.
        /// 
        /// </summary>
        /// <typeparam name="T">Type of the value returned for the key.</typeparam>
        /// <param name="key">Config key</param>
        /// <param name="defaultValue">If no value is configured, the value to use instead.</param>
        /// <returns>The configured value, or defaultValue if no configured value is set.</returns>
        /// <throws>InvalidOperationException if value cannot be cast to Type T.</throws>
        T GetConfig<T>(string key, T defaultValue);
        
        /// <summary>
        /// Invokes the onConfigChange callback when the provided key's configured value changes.
        /// 
        /// Implementations are responsible for ensuring change handlers do not get garbage
        /// collected.
        /// 
        /// If the configured value cannot be cast to Type T, the implementation should throw an
        /// InvalidOperationException with detailed information about the cast failure.
        /// </summary>
        /// <typeparam name="T">Type of the value for the key</typeparam>
        /// <param name="key">Config key</param>
        /// <param name="onConfigChange">Callback to invoke if config value changes</param>
        /// /// <throws>InvalidOperationException if value cannot be cast to Type T.</throws>
        void AddChangeHandler<T>(string key, Action<T> onConfigChange);

        // TODO run an initial test for these assumptions when implementations are passed to CommandInvoker.
    }
}
