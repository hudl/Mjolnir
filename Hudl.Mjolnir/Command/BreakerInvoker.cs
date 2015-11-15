using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Command;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    internal interface IBreakerInvoker
    {
        Task<TResult> ExecuteWithBreakerAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);
        TResult ExecuteWithBreaker<TResult>(SyncCommand<TResult> command, CancellationToken ct);
    }

    internal class BreakerInvoker
    {
        private async Task<TResult> ExecuteWithBreakerAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            var breaker = CommandContext.GetCircuitBreaker(command.BreakerKey);

            if (!breaker.IsAllowing())
            {
                throw new CircuitBreakerRejectedException();
            }

            TResult result;

            try
            {
                var stopwatch = Stopwatch.StartNew();

                // Await here so we can catch the Exception and track the state.
                // I suppose we could do this with a continuation, too. Await's easier.
                result = await command.ExecuteAsync(ct);

                breaker.MarkSuccess(stopwatch.ElapsedMilliseconds);
                breaker.Metrics.MarkCommandSuccess();
            }
            catch (Exception e)
            {
                if (CommandContext.IsExceptionIgnored(e.GetType()))
                {
                    breaker.Metrics.MarkCommandSuccess();
                }
                else
                {
                    breaker.Metrics.MarkCommandFailure();
                }

                throw;
            }

            return result;
        }

        private TResult ExecuteWithBreaker<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            var breaker = CommandContext.GetCircuitBreaker(command.BreakerKey);

            if (!breaker.IsAllowing())
            {
                throw new CircuitBreakerRejectedException();
            }

            TResult result;

            try
            {
                var stopwatch = Stopwatch.StartNew();

                result = command.Execute(ct);

                breaker.MarkSuccess(stopwatch.ElapsedMilliseconds);
                breaker.Metrics.MarkCommandSuccess();
            }
            catch (Exception e)
            {
                if (CommandContext.IsExceptionIgnored(e.GetType()))
                {
                    breaker.Metrics.MarkCommandSuccess();
                }
                else
                {
                    breaker.Metrics.MarkCommandFailure();
                }

                throw;
            }

            return result;
        }
    }
}
