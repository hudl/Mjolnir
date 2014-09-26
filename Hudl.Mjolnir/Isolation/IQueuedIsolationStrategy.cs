namespace Hudl.Mjolnir.Isolation
{
    /// <summary>
    /// Isolate a group of operations from the rest of the system, queuing them for work in some
    /// limited-resource manner (e.g. thread pool, counting semaphore).
    /// 
    /// Implementations should allow clients to try to queue items for processing, but should be
    /// proactive about disallowing operations when the pool (and its queue) are at capacity.
    /// 
    /// Operations within the system should be partitioned into logical groups (for example, based on
    /// downstream dependencies, or a common remote endpoint). Each group should use its own
    /// IQueuedIsolationStrategy; if one group begins to experience latency, back up, and reach capacity,
    /// further operations in that group should be rejected instead of blocking more resources
    /// (i.e. threads) that could instead be used for operations that aren't in the group and are
    /// successfully completing.
    /// </summary>
    internal interface IQueuedIsolationStrategy
    {
        // TODO Less necessary if we don't use a thread pool, but can be a no-op.
        //   - Possibly split this into a separate interface and/or push it to a factory.
        void Start();

        /// <summary>
        /// Queues an operation for execution. If the queue (if available) and concurrency-limiting mechanism are at
        /// capacity, may throw an <see cref="IsolationStrategyRejectedException">IsolationStrategyRejectedException</see>.
        /// </summary>
        /// <typeparam name="TResult">The type of result that will be returned from the work item.</typeparam>
        /// <param name="func">Operation to execute when the strategy implementation dequeues it.</param>
        /// <returns>A work item whose <code>Get()</code> method will return the <code>TResult</code>.</returns>
        IWorkItem<TResult> Enqueue<TResult>(System.Func<TResult> func);
    }
}
