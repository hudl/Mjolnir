using Hudl.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    internal interface IBulkheadInvoker
    {
        Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);
        TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct);
    }

    internal class BulkheadInvoker : IBulkheadInvoker
    {
        protected static readonly IConfigurableValue<bool> UseCircuitBreakers = new ConfigurableValue<bool>("mjolnir.useCircuitBreakers", true);

        private readonly IBreakerInvoker _breakerInvoker;

        public BulkheadInvoker(IBreakerInvoker breakerInvoker)
        {
            if (breakerInvoker == null)
            {
                throw new ArgumentNullException("breakerInvoker");
            }

            _breakerInvoker = breakerInvoker;
        }

        public async Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            // REWRITE:
            // - Get the semaphore bulkhead for the command group
            // - Reject or increment accordingly.

            // TODO get bulkhead and check; reject if necessary
            try
            {
                // TODO increment bulkhead
                return UseCircuitBreakers.Value
                    ? await _breakerInvoker.ExecuteWithBreakerAsync(command, ct)
                    : await command.ExecuteAsync(ct);
            }
            catch (Exception e)
            {
                // TODO decrement bulkhead
                throw;
            }

            // Note: Thread pool rejections shouldn't count as failures to the breaker.
            // If a downstream dependency is slow, the pool will fill up, but the
            // breaker + timeouts will already be providing protection against that.
            // If the pool is filling up because of a surge of requests, the rejections
            // will just be a way of shedding load - the breaker and downstream
            // dependency may be just fine, and we want to keep them that way.

            // We'll neither mark these as success *nor* failure, since they really didn't
            // even execute as far as the breaker and downstream dependencies are
            // concerned.

            //var workItem = ThreadPool.Enqueue(() =>
            //{
            //    var token = TimeoutsIgnored
            //        ? CancellationToken.None
            //        : cancellationToken;
            //    // Since we may have been on the thread pool queue for a bit, see if we
            //    // should have canceled by now.
            //    token.ThrowIfCancellationRequested();
            //    return UseCircuitBreakers.Value
            //        ? ExecuteWithBreaker(token)
            //        : ExecuteAsync(token);
            //});

            // We could avoid passing both the token and timeout if either:
            // A. SmartThreadPool.GetResult() took a CancellationToken.
            // B. The CancellationToken provided an accessor for its Timeout.
            // C. We wrapped CancellationToken and Timeout in another class and passed it.
            // For now, this works, if a little janky.
            //using high timeout (can't use Timespan.MaxValue since this overflows) and no cancellation token when timeouts are ignored, best thing to do without changing the IWorkItem interface
            //return TimeoutsIgnored
            //    ? workItem.Get(CancellationToken.None, TimeSpan.FromMilliseconds(int.MaxValue))
            //    : workItem.Get(cancellationToken, Timeout);
        }

        public TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            // REWRITE:
            // - Get the semaphore bulkhead for the command group
            // - Reject or increment accordingly.

            // TODO get bulkhead and check; reject if necessary
            try
            {
                // TODO increment bulkhead
                return UseCircuitBreakers.Value
                    ? _breakerInvoker.ExecuteWithBreaker(command, ct)
                    : command.Execute(ct);
            }
            catch (Exception e)
            {
                // TODO decrement bulkhead
                throw;
            }
        }
    }
}
