﻿using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Bulkhead;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// Executes a command on a circuit breaker.
    /// </summary>
    internal interface IBreakerInvoker
    {
        Task<TResult> ExecuteWithBreakerAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);
        TResult ExecuteWithBreaker<TResult>(SyncCommand<TResult> command, CancellationToken ct);
    }

    internal class BreakerInvoker : IBreakerInvoker
    {
        private readonly ICommandContext _context;
        private readonly IBreakerExceptionHandler _ignoredExceptions;

        public BreakerInvoker(ICommandContext context, IBreakerExceptionHandler ignoredExceptions)
        {
            _context = context ?? CommandContext.Current;

            if (ignoredExceptions == null)
            {
                throw new ArgumentNullException(nameof(ignoredExceptions));
            }

            _ignoredExceptions = ignoredExceptions;
        }

        public async Task<TResult> ExecuteWithBreakerAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            var breaker = _context.GetCircuitBreaker(command.BreakerKey);

            if (!breaker.IsAllowing())
            {
                _context.MetricEvents.RejectedByBreaker(breaker.Name, command.Name);
                throw new CircuitBreakerRejectedException();
            }

            TResult result;

            var success = true;
            var breakerStopwatch = Stopwatch.StartNew();
            var executionStopwatch = Stopwatch.StartNew();
            try
            {
                // Await here so we can catch the Exception and track the state.
                result = await command.ExecuteAsync(ct).ConfigureAwait(false);
                executionStopwatch.Stop();

                breaker.MarkSuccess(breakerStopwatch.ElapsedMilliseconds);
                breaker.Metrics.MarkCommandSuccess();
            }
            catch (Exception e)
            {
                executionStopwatch.Stop();
                success = false;

                if (_ignoredExceptions.IsExceptionIgnored(e.GetType()))
                {
                    success = true;
                    breaker.MarkSuccess(breakerStopwatch.ElapsedMilliseconds);
                    breaker.Metrics.MarkCommandSuccess();
                }
                else
                {
                    breaker.Metrics.MarkCommandFailure();
                }

                throw;
            }
            finally
            {
                command.ExecutionTimeMillis = executionStopwatch.Elapsed.TotalMilliseconds;

                if (success)
                {
                    _context.MetricEvents.BreakerSuccessCount(breaker.Name, command.Name);
                }
                else
                {
                    _context.MetricEvents.BreakerFailureCount(breaker.Name, command.Name);
                }
            }

            return result;
        }

        public TResult ExecuteWithBreaker<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            var breaker = _context.GetCircuitBreaker(command.BreakerKey);

            if (!breaker.IsAllowing())
            {
                _context.MetricEvents.RejectedByBreaker(breaker.Name, command.Name);
                throw new CircuitBreakerRejectedException();
            }

            TResult result;

            var success = true;
            var breakerStopwatch = Stopwatch.StartNew();
            var executionStopwatch = Stopwatch.StartNew();
            try
            {
                result = command.Execute(ct);
                executionStopwatch.Stop();

                breaker.MarkSuccess(breakerStopwatch.ElapsedMilliseconds);
                breaker.Metrics.MarkCommandSuccess();
            }
            catch (Exception e)
            {
                executionStopwatch.Stop();
                success = false;

                if (_ignoredExceptions.IsExceptionIgnored(e.GetType()))
                {
                    success = true;
                    breaker.MarkSuccess(breakerStopwatch.ElapsedMilliseconds);
                    breaker.Metrics.MarkCommandSuccess();
                }
                else
                {
                    breaker.Metrics.MarkCommandFailure();
                }

                throw;
            }
            finally
            {
                command.ExecutionTimeMillis = executionStopwatch.Elapsed.TotalMilliseconds;

                if (success)
                {
                    _context.MetricEvents.BreakerSuccessCount(breaker.Name, command.Name);
                }
                else
                {
                    _context.MetricEvents.BreakerFailureCount(breaker.Name, command.Name);
                }
            }

            return result;
        }
    }
}
