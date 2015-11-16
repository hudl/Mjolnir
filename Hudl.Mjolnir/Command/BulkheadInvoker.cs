using Hudl.Config;
using Hudl.Mjolnir.Bulkhead;
using System;
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

        // Note: Bulkhead rejections shouldn't count as failures to the breaker. If a downstream
        // dependency is slow, the pool will fill up, but the breaker + timeouts will already be
        // providing protection against that. If the bulkhead is filling up because of a surge of
        // requests, the rejections will just be a way of shedding load - the breaker and
        // downstream dependency may be just fine, and we want to keep them that way.

        // We'll neither mark these as success *nor* failure, since they really didn't even execute
        // as far as the breaker and downstream dependencies are concerned.

        public async Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            // TODO stats/events

            var bulkhead = CommandContext.GetBulkhead(command.BulkheadKey);

            if (!bulkhead.TryEnter())
            {
                throw new BulkheadRejectedException();
            }

            try
            {
                return UseCircuitBreakers.Value
                    ? await _breakerInvoker.ExecuteWithBreakerAsync(command, ct)
                    : await command.ExecuteAsync(ct);
            }
            finally
            {
                bulkhead.Release();
            }
        }

        public TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            var bulkhead = CommandContext.GetBulkhead(command.BulkheadKey);

            if (!bulkhead.TryEnter())
            {
                throw new BulkheadRejectedException();
            }

            try
            {
                return UseCircuitBreakers.Value
                    ? _breakerInvoker.ExecuteWithBreaker(command, ct)
                    : command.Execute(ct);
            }
            finally
            {
                bulkhead.Release();
            }
        }
    }
}
