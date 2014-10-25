using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;

namespace Hudl.Mjolnir.Isolation
{
    // This implementation is a modified version of one from the Parallel Extensions
    // Extras project: https://code.msdn.microsoft.com/ParExtSamples
    // TODO This is MS-LPL, check our license compliance and include MS-LPL if needed.

    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while
    /// running on top of the ThreadPool.
    /// </summary>
    public class LimitedConcurrencyLevelTaskScheduler : TaskScheduler
    {
        /// <summary>Whether the current thread is processing work items.</summary>
        [ThreadStatic]
        private static bool _currentThreadIsProcessingItems;
        
        /// <summary>The list of tasks to be executed.</summary>
        private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)
        
        /// <summary>The maximum concurrency level allowed by this scheduler.</summary>
        private readonly IConfigurableValue<int> _maxDegreeOfParallelism;

        /// <summary>The number of items allowed to be queued before the scheduler starts rejecting them.</summary>
        private readonly IConfigurableValue<int> _maxQueueLength;

        /// <summary>Whether the scheduler is currently processing work items.</summary>
        private int _delegatesQueuedOrRunning = 0; // protected by lock(_tasks)

        /// <summary>The number of tasks currently queued or executing on any thread. Includes tasks queued in _tasks.</summary>
        private int _tasksPendingCompletion = 0; // protected by lock(_tasks)

        /// <summary>
        /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
        /// specified degree of parallelism.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">The maximum degree of parallelism provided by this scheduler.</param>
        /// <param name="maxQueueLength">The maximum number of tasks to queue up for execution before throwing.</param>
        public LimitedConcurrencyLevelTaskScheduler(IConfigurableValue<int> maxDegreeOfParallelism, IConfigurableValue<int> maxQueueLength)
        {
            if (maxDegreeOfParallelism == null) throw new ArgumentNullException("maxDegreeOfParallelism");
            if (maxQueueLength == null) throw new ArgumentNullException("maxQueueLength");

            if (maxDegreeOfParallelism.Value < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism", "Initial value needs to be greater than 0");
            if (maxQueueLength.Value < 0) throw new ArgumentOutOfRangeException("maxQueueLength", "Initial value needs to be at least 0");

            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _maxQueueLength = maxQueueLength;
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        /// <exception cref="QueueLengthExceededException">If the scheduler queue is at its maximum.</exception>
        protected sealed override void QueueTask(Task task)
        {
            // Add the task to the list of tasks to be processed.  If there aren't enough
            // delegates currently queued or running to process tasks, schedule another.
            lock (_tasks)
            {
                // Store config values locally in case the config changes, maintaining
                // consistency with the values used within this method.
                var maxParallelism = _maxDegreeOfParallelism.Value;
                var maxQueueLength = _maxQueueLength.Value;

                var currentQueueLength = _tasks.Count;

                // The queue maximum isn't a strict maximum on the List<Task> itself. It's really a limit on
                // how many we'll queue when the concurrency is hit. There may be periods of rapid queueing
                // that push the list beyond the queue maximum, and that's okay, as long as we don't:
                //   1. Execute more than _maxDegreeOfParallelism at any given time, and
                //   2. Queue beyond _maxQueueLength when we're running at max parallelism
                if (_tasksPendingCompletion >= maxParallelism + maxQueueLength)
                {
                    // Note that (_tasksPendingCompletion - currentQueueLength != tasks currently in flight). In fact,
                    // there's the possibility that _tasksPendingCompletion == currentQueueLength (the condition where
                    // many items were rapidly queued before any were able to be pulled off for execution).
                    throw new QueueLengthExceededException(_tasksPendingCompletion, currentQueueLength);
                }

                ++_tasksPendingCompletion;
                _tasks.AddLast(task);
                if (_delegatesQueuedOrRunning < maxParallelism)
                {
                    // Clarification: _delegatesQueuedOrRunning *isn't* the number of currently-executing
                    // Tasks, it's the number of different ThreadPool Queue calls that we've dispatched
                    // off for concurrent processing.
                    ++_delegatesQueuedOrRunning;
                    NotifyThreadPoolOfPendingWork();
                }
            }
        }

        /// <summary>
        /// Informs the ThreadPool that there's work to be executed for this scheduler.
        /// </summary>
        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                // Note that the current thread is now processing work items.
                // This is necessary to enable inlining of tasks into this thread.
                _currentThreadIsProcessingItems = true;
                try
                {
                    // Process all available items in the queue.
                    while (true)
                    {
                        Task item;
                        lock (_tasks)
                        {
                            // When there are no more items to be processed,
                            // note that we're done processing, and get out.
                            if (_tasks.Count == 0)
                            {
                                --_delegatesQueuedOrRunning;
                                break;
                            }

                            // Get the next item from the queue
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        // Execute the task we pulled out of the queue
                        try
                        {
                            TryExecuteTask(item);
                        }
                        finally
                        {
                            lock (_tasks)
                            {
                                --_tasksPendingCompletion;
                            }
                        }
                    }
                }
                // We're done processing items on the current thread
                finally { _currentThreadIsProcessingItems = false; }
            }, null);
        }

        /// <summary>Attempts to execute the specified task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns>Whether the task could be executed on the current thread.</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // If this thread isn't already processing a task, we don't support inlining
            if (!_currentThreadIsProcessingItems) return false;

            // If the task was previously queued, remove it from the queue
            if (taskWasPreviouslyQueued) TryDequeue(task);

            // Try to run the task.
            return TryExecuteTask(task);
        }

        /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
        /// <param name="task">The task to be removed.</param>
        /// <returns>Whether the task could be found and removed.</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) return _tasks.Remove(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism.Value; } }

        /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
        /// <returns>An enumerable of the tasks currently scheduled.</returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) return _tasks.ToArray();
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(_tasks);
            }
        }
    }

    internal sealed class QueueLengthExceededException : Exception
    {
        internal const string PendingCompletionDataKey = "PendingCompletion";
        internal const string CurrentlyQueuedDataKey = "CurrentlyQueued";

        public QueueLengthExceededException(int pendingCompletion, int currentlyQueued)
        {
            Data[PendingCompletionDataKey] = pendingCompletion;
            Data[CurrentlyQueuedDataKey] = currentlyQueued;
        }
    }
}
