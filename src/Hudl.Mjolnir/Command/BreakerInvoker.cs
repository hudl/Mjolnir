using Elastic.Apm.Api;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Util;
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
        private readonly ICircuitBreakerFactory _circuitBreakerFactory;
        private readonly IMetricEvents _metricEvents;
        private readonly IBreakerExceptionHandler _ignoredExceptions;

        public BreakerInvoker(ICircuitBreakerFactory circuitBreakerFactory, IMetricEvents metricEvents, IBreakerExceptionHandler ignoredExceptions)
        {
            _circuitBreakerFactory = circuitBreakerFactory ?? throw new ArgumentNullException(nameof(circuitBreakerFactory));
            _metricEvents = metricEvents ?? throw new ArgumentNullException(nameof(metricEvents));
            _ignoredExceptions = ignoredExceptions ?? throw new ArgumentNullException(nameof(ignoredExceptions));
        }

        public async Task<TResult> ExecuteWithBreakerAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            ISpan span = Elastic.Apm.Agent.Tracer.CurrentTransaction?.StartSpan("ExecuteWithBreakerAsync", ApiConstants.TypeDb, ApiConstants.SubtypeMssql, ApiConstants.ActionQuery);
            try
            {
                //application code that is captured as a transaction

                var breaker = _circuitBreakerFactory.GetCircuitBreaker(command.BreakerKey);

                if (!breaker.IsAllowing())
                {
                    MjolnirEventSource.Log.CircuitBreakerRejection(breaker.Name, command.Name);
                    _metricEvents.RejectedByBreaker(breaker.Name, command.Name);
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
                        _metricEvents.BreakerSuccessCount(breaker.Name, command.Name);
                    }
                    else
                    {
                        _metricEvents.BreakerFailureCount(breaker.Name, command.Name);
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                if (span != null) span.CaptureException(e);
                throw;
            }
            finally
            {
                if (span != null) span.End();
            }
        }

        public TResult ExecuteWithBreaker<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            var breaker = _circuitBreakerFactory.GetCircuitBreaker(command.BreakerKey);

            if (!breaker.IsAllowing())
            {
                _metricEvents.RejectedByBreaker(breaker.Name, command.Name);
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
                    _metricEvents.BreakerSuccessCount(breaker.Name, command.Name);
                }
                else
                {
                    _metricEvents.BreakerFailureCount(breaker.Name, command.Name);
                }
            }

            return result;
        }
    }
}
