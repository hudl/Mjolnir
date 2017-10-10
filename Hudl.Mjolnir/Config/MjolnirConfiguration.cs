using System.Collections.Generic;

namespace Hudl.Mjolnir.Config
{
    /// <summary>
    /// Abstract class implementation for config values.
    /// This is used to instantiate all mjolnir configuration values. 
    /// </summary>
    public abstract class MjolnirConfiguration
    {
        /// <summary>
        /// Global Killswitch - Mjolnir can be turned off entirely if needed (though it's certainly not recommended). 
        /// If isEnabled is set to false, Mjolnir will still do some initial work (like ensuring a single invoke per 
        /// Command), but will then just execute the Command (calling Execute() or ExecuteAsync()) instead of passing 
        /// it through Bulkheads and Circuit Breakers. No timeouts will be applied; a CancellationToken.None will be 
        /// passed to any method that supports cancellation.
        /// </summary>
        public bool IsEnabled { get; protected set; }
        
        
        /// <summary>
        /// Global Ignore - Timeouts can be globally ignored. Only recommended for use in local/testing environments.
        /// </summary>
        public bool IgnoreTimeouts { get; protected set; }
        
        
        /// <summary>
        /// Default Timeouts for commands.
        /// </summary>
        protected CommandConfiguration DefaultCommandConfiguration;
        
        
        /// <summary>
        /// Configuring Timeouts for commands.
        /// </summary>
        protected Dictionary<string, CommandConfiguration> CommandConfigurations;
        

       
        /// <summary>
        /// System-wide default. Used if a per-bulkhead config isn't configured.
        /// </summary>
        protected BulkheadConfiguration DefaultBulkheadConfiguration { get; set; }
        
        /// <summary>
        /// Per-bulkhead configuration. Key is the argument passed to the Command constructor.
        /// </summary>
        protected Dictionary<string, BulkheadConfiguration> BulkheadConfigurations { get; set; }

      
        
        /// <summary>
        /// Global Enable/Disable - Circuit Breakers can be globally disabled.
        /// </summary>
        public bool UseCircuitBreakers { get; protected set; }      

        /// <summary>
        /// System-wide default. Used if a per-breaker config isn't configured.
        /// </summary>
        protected BreakerConfiguration DefaultBreakerConfiguration { get; set; }
        
        /// <summary>
        /// Per-breaker configuration. breaker-key is the argument passed to the Command constructor.
        /// </summary>
        protected Dictionary<string, BreakerConfiguration> BreakerConfigurations { get; set; }
        
        
        /// <summary>
        /// Default constructor just to create dictionaires.
        /// </summary>
        protected MjolnirConfiguration()
        {
            CommandConfigurations = new Dictionary<string, CommandConfiguration>();
            DefaultCommandConfiguration = new CommandConfiguration();
            BulkheadConfigurations = new Dictionary<string, BulkheadConfiguration>();
            DefaultBulkheadConfiguration = new StaticBulkheadConfiguration();
            BreakerConfigurations = new Dictionary<string, BreakerConfiguration>();
            DefaultBreakerConfiguration = new BreakerConfiguration();
        }
        
        /// <summary>
        /// Gets command configuration for a given key. If key is null or not exists in configuration dictionary
        /// default configuration is being returned.
        /// </summary>
        /// <param name="groupKey">Command configuration for a given key. Default value returned if non-existent or 
        /// null.</param>
        /// <returns></returns>
        public CommandConfiguration GetCommandConfiguration(string groupKey = null)
        {
            if (groupKey == null) return DefaultCommandConfiguration;
            CommandConfiguration commandConfiguration;
            return CommandConfigurations.TryGetValue(groupKey, out commandConfiguration) ?
                commandConfiguration : DefaultCommandConfiguration;
        }
        
        /// <summary>
        /// Gets bulkhead configuration for a given key. If key is null or not exists in configuration dictionary 
        /// default configuration is being returned.
        /// </summary>
        /// <param name="key">Bulkhead configuration for a given key. Default value returned if non-existent or 
        /// null.</param>
        /// <returns></returns>
        public BulkheadConfiguration GetBulkheadConfiguration(string key = null)
        {
            if (key == null) return DefaultBulkheadConfiguration;
            BulkheadConfiguration bulkheadConfiguration;
            return BulkheadConfigurations.TryGetValue(key, out bulkheadConfiguration) ?
                bulkheadConfiguration : DefaultBulkheadConfiguration;
        }       
        
        
        /// <summary>
        /// Gets breaker configuration for a given key. If key is null or not exists in configuration dictionary 
        /// default configuration is being returned.
        /// </summary>
        /// <param name="key">Breaker configuration for a given key. Default value returned if non-existent or 
        /// null.</param>
        /// <returns></returns>
        public BreakerConfiguration GetBreakerConfiguration(string key = null)
        {
            if (key == null) return DefaultBreakerConfiguration;
            BreakerConfiguration breakerConfiguration;
            return BreakerConfigurations.TryGetValue(key, out breakerConfiguration) ?
                breakerConfiguration : DefaultBreakerConfiguration;
        }
        
    }
}