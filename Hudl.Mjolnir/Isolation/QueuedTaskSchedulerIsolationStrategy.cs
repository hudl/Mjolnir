using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;

namespace Hudl.Mjolnir.Isolation
{
    /// <summary>
    /// Isolates operations by using a concurrency-limiting, queue-driven TaskScheduler.
    /// </summary>
    internal class QueuedTaskSchedulerIsolationStrategy : IQueuedIsolationStrategy
    {
        // Custom TaskFactory that handles concurrency and task queueing.
        private readonly TaskFactory _factory;

        internal QueuedTaskSchedulerIsolationStrategy(IConfigurableValue<int> maxConcurrency, IConfigurableValue<int> maxQueueLength)
        {
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxConcurrency, maxQueueLength);
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
            catch (TaskSchedulerException e)
            {
                if (e.InnerException is QueueLengthExceededException)
                {
                    var f = new IsolationStrategyRejectedException("TaskScheduler is at maximum concurrency and queue size", e);
                    foreach (var key in e.InnerException.Data.Keys)
                    {
                        f.Data[key] = e.InnerException.Data[key];
                    }
                    throw f;
                }

                throw;
            }
        }
    }
}
