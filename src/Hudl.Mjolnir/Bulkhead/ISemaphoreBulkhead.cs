namespace Hudl.Mjolnir.Bulkhead
{
    /// <summary>
    /// Wraps a semaphore that's used for a command bulkhead. Bulkheads serve to limit concurrency
    /// of commands that pass through them, which can be useful when downstream systems are slow
    /// and causing client-side calls to get backed up, holding onto client system resources
    /// (connections, memory, etc.). Bulkheads will reject calls commands if the concurrency limit
    /// is exceeded.
    /// </summary>
    internal interface ISemaphoreBulkhead
    {

        /// <summary>
        /// Releases the semaphore. Make sure to call this on any semaphore acquired via
        /// TryEnter(). If TryEnter() fails (returns false), you don't need to Release().
        /// </summary>
        void Release();

        /// <summary>
        /// Tries to immediately enter the semaphore, returning true if entry was allowed.
        /// Does not wait or block.
        /// 
        /// If you use this method, be sure to call .Release() when done with the semaphore.
        /// </summary>
        bool TryEnter();

        /// <summary>
        /// The number of open spots available on the semaphore.
        /// </summary>
        int CountAvailable { get; }

        /// <summary>
        /// The name of the semaphore.
        /// </summary>
        string Name { get; }
    }
}
