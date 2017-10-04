namespace Hudl.Mjolnir.Config
{
    public class BulkheadsConfiguration
    {
        /// <summary>
        /// Bulkhead Maximum - The number of Commands that can execute in the Bulkhead concurrently before subsequent 
        /// Command attempts are rejected.
        /// </summary>
        public int MaxConcurrent { get; set; }

        public BulkheadsConfiguration()
        {
            // Set default value
            MaxConcurrent = 10;
        }
    }
}