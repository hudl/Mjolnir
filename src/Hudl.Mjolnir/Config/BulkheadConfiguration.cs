namespace Hudl.Mjolnir.Config
{
    public class BulkheadConfiguration
    {
        /// <summary>
        /// Bulkhead Maximum - The number of Commands that can execute in the Bulkhead concurrently before subsequent 
        /// Command attempts are rejected.
        /// </summary>
        public virtual int MaxConcurrent { get; set; }

        /// <summary>
        /// This flag will stop a BulkheadRejectionException being thrown and will continue to execute on the command. 
        /// However the IMetricsEvents.RejectedByBulkhead() method will still be invoked. The main purpose of this 
        /// configuration is to enable diagnostics and greater visibility into the behaviour of a system so that 
        /// bulkhead concurrency levels can be tuned more easily. 
        /// </summary>
        /// <returns></returns>
        public virtual bool MetricsOnly { get; set; }

        public BulkheadConfiguration()
        {
            // Set default value
            MaxConcurrent = 10;
        }
    }
}