namespace Hudl.Mjolnir.ThreadPool
{
    /// <summary>
    /// A thread pool whose purposes is to isolate a group of operations from the rest of the system.
    /// 
    /// Implementations should allow clients to try to queue items for processing, but should be
    /// proactive about disallowing operations when the pool (and its queue) are at capacity.
    /// 
    /// Operations within the system should be partitioned into logical groups (for example, based on
    /// downstream dependencies, or a common remote endpoint). Each group should use its own
    /// IIsolationThreadPool; if one group begins to experience latency, back up, and reach capacity,
    /// further operations in that group should be rejected instead of blocking more resources
    /// (i.e. threads) that could instead be used for operations that aren't in the group and are
    /// successfully completing.
    /// </summary>
    internal interface IIsolationThreadPool
    {
        void Start();

        /// <summary>
        /// Queues the function for execution on the pool. If the pool and queue (if available) are at
        /// capacity, may throw an <see cref="IsolationThreadPoolRejectedException">IsolationThreadPoolRejectedException</see>.
        /// </summary>
        /// <typeparam name="TResult">The type of result that will be returned from the work item.</typeparam>
        /// <param name="func">Function to execute on the pool thread.</param>
        /// <returns>A work item whose <code>Get()</code> method will return the <code>TResult</code>.</returns>
        IWorkItem<TResult> Enqueue<TResult>(System.Func<TResult> func);
    }
}
