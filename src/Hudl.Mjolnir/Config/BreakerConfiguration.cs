namespace Hudl.Mjolnir.Config
{
    public class BreakerConfiguration
    {
        /// <summary>
        /// Counting Window - A Breakers monitors error counts within a short, non-rolling window of time, resetting 
        /// counts when the window ends.
        /// </summary>
        public long WindowMillis { get; set; }
        
        /// <summary>
        /// Minimum Operations - a Breaker won't trip until it sees at least this many operations come through in the 
        /// configured windowMillis.
        /// </summary>
        public int MinimumOperations { get; set; }
        
        /// <summary>
        /// Threshold Percentage - If the error rate within the window meets or exceeds this percentage, the Breaker 
        /// will trip.
        /// </summary>
        public int ThresholdPercentage { get; set; }
        
        
        /// <summary>
        /// Tripped Duration - When the Breaker trips, it will wait this long before attempting a test operation to see 
        /// if it should close and fix itself.
        /// </summary>
        public long TrippedDurationMillis { get; set; }
        
        /// <summary>
        /// Force Tripped - Forces a Breaker tripped (open), regardless of its current error count. If both (tripped and 
        /// fixed) are true, the Breaker will be tripped.
        /// </summary>
        public bool ForceTripped { get; set; }
        
        
        /// <summary>
        /// Force Fixed - Forces a Breaker fixed (closed), regardless of its current error count. If both (tripped and 
        /// fixed) are true, the Breaker will be tripped.
        /// </summary>
        public bool ForceFixed { get; set; }

        public long SnapshotTtlMillis { get; set; }
    }
}