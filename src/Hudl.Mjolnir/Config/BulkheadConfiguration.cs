namespace Hudl.Mjolnir.Config
{
    public class BulkheadConfiguration
    {
        /// <summary>
        /// Bulkhead Maximum - The number of Commands that can execute in the Bulkhead concurrently before subsequent 
        /// Command attempts are rejected.
        /// </summary>
        public virtual int MaxConcurrent { get; set; }

        public BulkheadConfiguration()
        {
            // Set default value
            MaxConcurrent = 10;
        }
    }
}