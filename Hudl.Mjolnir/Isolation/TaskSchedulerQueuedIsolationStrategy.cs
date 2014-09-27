using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;

namespace Hudl.Mjolnir.Isolation
{
    /// <summary>
    /// Isolates operations by using a concurrency-limiting TaskScheduler.
    /// </summary>
    internal class TaskSchedulerQueuedIsolationStrategy : IQueuedIsolationStrategy
    {
        // Custom TaskFactory that handles concurrency and task queueing.
        private readonly TaskFactory _factory;

        internal TaskSchedulerQueuedIsolationStrategy(IConfigurableValue<int> maxConcurrency, IConfigurableValue<int> maxQueueLength)
        {
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxConcurrency.Value, maxQueueLength.Value);
            _factory = new TaskFactory(scheduler);
        }

        public Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken)
        {
            try
            {
                // Should throw if the scheduler is at max concurrency and
                // its queue is also at capacity.
                return _factory.StartNew(func, cancellationToken);
            }
            catch (QueueLengthExceededException e)
            {
                // Hide the TaskScheduler implementation by wrapping with an
                // isolation-specific exception.
                ExceptionDispatchInfo.Capture(e).Throw();
                throw; // Should never get here.
            }
        }
    }
}
