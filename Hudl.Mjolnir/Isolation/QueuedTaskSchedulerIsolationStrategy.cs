using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Util;

namespace Hudl.Mjolnir.Isolation
{
    /// <summary>
    /// Isolates operations by using a concurrency-limiting, queue-driven TaskScheduler.
    /// </summary>
    internal class QueuedTaskSchedulerIsolationStrategy : IQueuedIsolationStrategy
    {
        private readonly GroupKey _key;
        private readonly IStats _stats;

        // Custom TaskFactory that handles concurrency and task queueing.
        private readonly TaskFactory _factory;

        internal QueuedTaskSchedulerIsolationStrategy(GroupKey key, IConfigurableValue<int> maxConcurrency, IConfigurableValue<int> maxQueueLength, IStats stats)
        {
            _key = key;
            _stats = stats;
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxConcurrency, maxQueueLength);

            // Hide the scheduler we're using here (using TaskCreationOptions.HideScheduler)
            // from any new Tasks spawned by the actions we Enqueue(). We'll just let them
            // use the default scheduler. If they turn out to be other commands, they'll get
            // isolated when they're executed by their own command.
            _factory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler, TaskContinuationOptions.None, scheduler);
        }

        private string StatsPrefix
        {
            get { return "mjolnir pool " + _key; }
        }

        public Task Enqueue(Action action, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var state = "Enqueued";
            try
            {
                return _factory.StartNew(action, cancellationToken);
            }
            catch (TaskSchedulerException e)
            {
                // A QueueLengthExceededException is the only special case - it means
                // that we're hitting our limits and should reject the operation.
                // Wrap it in an isolation-specific exception so callers can react.
                if (e.InnerException is QueueLengthExceededException)
                {
                    state = "Rejected";
                    var f = new IsolationStrategyRejectedException("TaskScheduler is at maximum concurrency and queue size", e);
                    foreach (var key in e.InnerException.Data.Keys)
                    {
                        f.Data[key] = e.InnerException.Data[key];
                    }
                    throw f;
                }
                throw;
            }
            finally
            {
                _stats.Elapsed(StatsPrefix + " Enqueue", state, stopwatch.Elapsed);
            }
        }

        public Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var state = "Enqueued";
            try
            {
                return _factory.StartNew(func, cancellationToken);
            }
            catch (TaskSchedulerException e)
            {
                // A QueueLengthExceededException is the only special case - it means
                // that we're hitting our limits and should reject the operation.
                // Wrap it in an isolation-specific exception so callers can react.
                if (e.InnerException is QueueLengthExceededException)
                {
                    state = "Rejected";
                    var f = new IsolationStrategyRejectedException("TaskScheduler is at maximum concurrency and queue size", e);
                    foreach (var key in e.InnerException.Data.Keys)
                    {
                        f.Data[key] = e.InnerException.Data[key];
                    }
                    throw f;
                }
                throw;
            }
            finally
            {
                _stats.Elapsed(StatsPrefix + " Enqueue", state, stopwatch.Elapsed);
            }
        }
    }
}
