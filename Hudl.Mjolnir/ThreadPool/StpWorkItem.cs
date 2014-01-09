using System;
using System.Threading;
using Amib.Threading;

namespace Hudl.Mjolnir.ThreadPool
{
    /// <summary>
    /// WorkItem implementation for SmartThreadPool work items.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class StpWorkItem<TResult> : IWorkItem<TResult>
    {
        private readonly IWorkItemResult<TResult> _workItem;

        public StpWorkItem(IWorkItemResult<TResult> workItem)
        {
            _workItem = workItem;
        }

        /// <summary>
        /// Blocks until a result is available, or the work item is canceled or times out.
        /// </summary>
        /// <param name="cancellationToken">Token to use for cancellation.</param>
        /// <param name="timeout">Timeout for the work item; should have come from the timeout the cancellation token is using.</param>
        /// <returns>The result of the work item processing.</returns>
        public TResult Get(CancellationToken cancellationToken, TimeSpan timeout)
        {
            try
            {
                // GetResult() blocks until result is available (or item is cancelled, etc.)
                return _workItem.GetResult(timeout, false);
            }
            catch (Exception e)
            {
                // If the item itself threw an exception, re-throw it (instead of the wrapper).
                if (e is WorkItemResultException)
                {
                    // I'd like to not wrap here, but without it we'll lose the trace.
                    // TODO Might be worth re-thinking the exception chain here. Can CommandFailedException have an ExecuteException property or something that we can just assign the root cause to?
                    throw new IsolationThreadPoolException(e);
                }

                // If the caller cancelled via the token before calling
                // ExecuteAsync() or during its execution, it will have thrown an
                // OperationCanceledException() that'll get picked up by the
                // WorkItemResultException above.

                if (e is WorkItemCancelException || e is WorkItemTimeoutException)
                {
                    // Note that this was probably caused by the timeout that's used by the
                    // CancellationToken up in the command, but this OperationCanceledException won't
                    // be associated with that token. Upstream exception handling shouldn't assume
                    // that the OCE is or isn't a result of the token expiration solely on the token's
                    // association with the Exception.
                    throw new OperationCanceledException(e.Message, e);
                }

                throw new IsolationThreadPoolException(e);
            }
            finally
            {
                // If we blocked on GetResult() for too long, just cancel.
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}