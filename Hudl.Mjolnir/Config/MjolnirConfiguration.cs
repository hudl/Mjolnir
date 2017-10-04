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
        public bool IsEnabled { get; set; }
        
        
        /// <summary>
        /// Global Ignore - Timeouts can be globally ignored. Only recommended for use in local/testing environments.
        /// </summary>
        public bool IgnoreTimeouts { get; set; }
        
        /// <summary>
        /// Configuring Timeouts for commands.
        /// </summary>
        public Dictionary<string, CommandConfiguration> CommandConfigurations { get; set; }
        
        
        /// <summary>
        /// System-wide default. Used if a per-bulkhead config isn't configured.
        /// </summary>
        public BulkheadsConfiguration DefaultBulkheadsConfiguration { get; set; }
        
        /// <summary>
        /// Per-bulkhead configuration. bulkhead-key is the argument passed to the Command constructor.
        /// </summary>
        public Dictionary<string, BulkheadsConfiguration> BulkheadsConfigurations { get; set; }
        
        
        /// <summary>
        /// Global Enable/Disable - Circuit Breakers can be globally disabled.
        /// </summary>
        public bool UseCircuitBreakers { get; set; }      

        /// <summary>
        /// System-wide default. Used if a per-breaker config isn't configured.
        /// </summary>
        public BreakerConfiguration DefaultBreakerConfiguration { get; set; }
        
        /// <summary>
        /// Per-breaker configuration. breaker-key is the argument passed to the Command constructor.
        /// </summary>
        public Dictionary<string, BreakerConfiguration> BreakerConfigurations { get; set; }
    }
}