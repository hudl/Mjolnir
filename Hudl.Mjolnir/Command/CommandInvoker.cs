﻿using Hudl.Common.Extensions;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.External;
using log4net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    public interface ICommandInvoker
    {
        Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, long? timeoutMillis = null);
        CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, long? timeoutMillis = null);

        // TODO possible alternate signatures:
        // 
        // Could forego the CommandResult wrapper, which would enforce a OnFailure.Throw
        //   TResult InvokeAndUnwrapOrThrow<TResult>(...)
    }

    // TODO add ConfigureAwait(false) where necessary
    // TODO what do timeouts/cancellations look like in exceptions now? make sure we didn't revert that logging change

    public class CommandInvoker : ICommandInvoker
    {
        private readonly IStats _stats;
        private readonly IBulkheadInvoker _bulkheadInvoker;

        public CommandInvoker()
        {
            _stats = CommandContext.Stats; // TODO any risk here? should we just DI this? possibly not.
            _bulkheadInvoker = new BulkheadInvoker(new BreakerInvoker()); // TODO clean this up
        }

        internal CommandInvoker(IStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException("stats");
            }

            _stats = stats ?? CommandContext.Stats; // TODO any init risk here?
            _bulkheadInvoker = new BulkheadInvoker(new BreakerInvoker()); // TODO clean this up
        }

        public async Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, long? timeoutMillis = null)
        {
            // This doesn't adhere to the OnFailure action because it's a bug in the code
            // and should always throw so people see it and fix it.
            EnsureSingleInvoke(command);
            
            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var timeout = command.GetActualTimeout(timeoutMillis);

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;
            
            var cts = timeout.HasValue
                ? new CancellationTokenSource(timeout.Value)
                : new CancellationTokenSource();

            try
            {
                // TODO renamed "InvokeAsync" in the log here, should be documented.
                log.InfoFormat("Invoke Command={0} Breaker={1} Pool={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    GetTimeoutForLog(timeout));

                var result = await _bulkheadInvoker.ExecuteWithBulkheadAsync(command, cts.Token).ConfigureAwait(false);
                executeStopwatch.Stop();
                return new CommandResult<TResult>(result, status);
            }
            catch (Exception e)
            {
                executeStopwatch.Stop();

                // TODO document new behavior here - exceptions aren't wrapped anymore. fallbacks removed.
                status = GetCompletionStatus(e, cts);
                AttachCommandExceptionData(command, e, status, timeout);

                if (failureAction == OnFailure.Throw)
                {
                    throw;
                }

                return new CommandResult<TResult>(default(TResult), status, e);
            }
            finally
            {
                invokeStopwatch.Stop();

                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), executeStopwatch.Elapsed);
                _stats.Elapsed(command.StatsPrefix + " total", status.ToString(), invokeStopwatch.Elapsed);
            }
        }

        public CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, long? timeoutMillis = null)
        {
            // This doesn't adhere to the OnFailure action because it's a bug in the code
            // and should always throw so people see it and fix it.
            EnsureSingleInvoke(command);
            
            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var timeout = command.GetActualTimeout(timeoutMillis);

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;

            var cts = timeout.HasValue
                ? new CancellationTokenSource(timeout.Value)
                : new CancellationTokenSource();

            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Pool={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    GetTimeoutForLog(timeout));

                var result = _bulkheadInvoker.ExecuteWithBulkhead(command, cts.Token);
                executeStopwatch.Stop();
                return new CommandResult<TResult>(result, status);
            }
            catch (Exception e)
            {
                executeStopwatch.Stop();

                // TODO document new behavior here - exceptions aren't wrapped anymore. fallbacks removed.
                status = GetCompletionStatus(e, cts);
                AttachCommandExceptionData(command, e, status, timeout);

                if (failureAction == OnFailure.Throw)
                {
                    throw;
                }

                return new CommandResult<TResult>(default(TResult), status, e);
            }
            finally
            {
                invokeStopwatch.Stop();

                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), executeStopwatch.Elapsed);
                _stats.Elapsed(command.StatsPrefix + " total", status.ToString(), invokeStopwatch.Elapsed);
            }
        }

        private void EnsureSingleInvoke(BaseCommand command)
        {
            if (Interlocked.CompareExchange(ref command._hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }
        }
        
        private static CommandCompletionStatus GetCompletionStatus(Exception exception, CancellationTokenSource cts)
        {
            if (IsCancellationException(exception))
            {
                // If the timeout cancellationTokenSource was cancelled and we got an TaskCancelledException here then this means the call actually timed out.
                // Otherwise an TaskCancelledException would have been raised if a user CancellationToken was passed through to the method call, and was explicitly
                // cancelled from the client side.
                if (cts.IsCancellationRequested)
                {
                    return CommandCompletionStatus.TimedOut;
                }

                return CommandCompletionStatus.Canceled;
            }

            if (exception is CircuitBreakerRejectedException || exception is BulkheadRejectedException)
            {
                return CommandCompletionStatus.Rejected;
            }

            return CommandCompletionStatus.Faulted;
        }

        private void AttachCommandExceptionData(BaseCommand command, Exception exception, CommandCompletionStatus status, TimeSpan? timeout)
        {
            // TODO document that "Pool" changed to "Bulkhead"

            exception.WithData(new
            {
                Command = command.Name,
                Timeout = GetTimeoutForLog(timeout),
                Status = status,
                Breaker = command.BreakerKey,
                Bulkhead = command.BulkheadKey,
            });
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }

        private static string GetTimeoutForLog(TimeSpan? timeout)
        {
            return (timeout.HasValue ? timeout.Value.ToString() : "Disabled");
        }
    }

    // Failure is any of [Fault || Timeout || Reject]
    public enum OnFailure
    {
        Throw,
        Return,
    }
    
    public sealed class CommandResult<TResult>
    {
        private readonly TResult _value;
        private readonly CommandCompletionStatus _status;
        private readonly Exception _exception;

        public TResult Value { get { return _value; } }
        public CommandCompletionStatus Status { get { return _status; } }
        public Exception Exception { get { return _exception; } }

        internal CommandResult(TResult value, CommandCompletionStatus status, Exception exception = null)
        {
            _value = value;
            _status = status;
            _exception = exception;
        }
    }
}
