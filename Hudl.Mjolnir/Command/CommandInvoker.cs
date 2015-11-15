using Hudl.Common.Extensions;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.ThreadPool;
using log4net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    public interface ICommandInvoker
    {
        Task<TResult> InvokeAsync<TResult>(AsyncCommand<TResult> command);
        TResult Invoke<TResult>(SyncCommand<TResult> command);
    }

    // TODO add ConfigureAwait(false) where necessary
    // TODO can breaker/pool/etc. be moved off of the Command object and into the invoker methods?
    // - yes, but what are the implications to unit testing?

    public class CommandInvoker : ICommandInvoker
    {
        private readonly IStats _stats;
        private readonly IBulkheadInvoker _bulkheadInvoker;

        public CommandInvoker()
        {
            _stats = CommandContext.Stats; // TODO any risk here? should we just DI this? possibly not.
        }

        internal CommandInvoker(IStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException("stats");
            }

            _stats = stats ?? CommandContext.Stats; // TODO any init risk here?
        }

        public async Task<TResult> InvokeAsync<TResult>(AsyncCommand<TResult> command)
        {
            if (Interlocked.CompareExchange(ref command._hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }

            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;

            var cts = command.Timeout.HasValue
                ? new CancellationTokenSource(command.Timeout.Value)
                : new CancellationTokenSource();

            try
            {
                // TODO renamed "InvokeAsync" in the log here, should be documented.
                log.InfoFormat("Invoke Command={0} Breaker={1} Pool={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.PoolKey,
                    command.Timeout.HasValue ? command.Timeout.Value.TotalMilliseconds.ToString() : "Disabled");

                // TODO soon, this comment may not be true
                // Note: this actually awaits the *enqueueing* of the task, not the task execution itself.
                var result = await _bulkheadInvoker.ExecuteWithBulkheadAsync(command, cts.Token).ConfigureAwait(false);
                executeStopwatch.Stop();
                return result;
            }
            catch (Exception e)
            {
                var tokenSourceCancelled = cts.IsCancellationRequested;
                executeStopwatch.Stop();
                var instigator = GetCommandFailedException(e, tokenSourceCancelled, out status).WithData(new
                {
                    Command = command.Name,
                    Timeout = (command.Timeout.HasValue ? command.Timeout.Value.TotalMilliseconds.ToString() : "Disabled"),
                    Status = status,
                    Breaker = command.BreakerKey,
                    Pool = command.PoolKey,
                });

                // We don't log the exception here - that's intentional.

                // If a fallback is not implemented, the exception will get re-thrown and (hopefully) caught
                // and logged by an upstream container. This is the majority of cases, so logging here
                // results in a lot of extra, unnecessary logs and stack traces.

                // If a fallback is implemented, the burden is on the implementation to log or rethrow the
                // exception. Otherwise it'll be eaten. This is documented on the Fallback() method.

                // TODO re-think fallbacks; what of async vs. sync support?
                // - should fallbacks be interface-driven, e.g. AsyncFallback / SyncFallback?

                return TryFallback(instigator);
            }
            finally
            {
                invokeStopwatch.Stop();

                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), executeStopwatch.Elapsed);
                _stats.Elapsed(command.StatsPrefix + " total", status.ToString(), invokeStopwatch.Elapsed);
            }
        }

        public TResult Invoke<TResult>(SyncCommand<TResult> command)
        {
            if (Interlocked.CompareExchange(ref command._hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }

            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;

            var cts = command.Timeout.HasValue
                ? new CancellationTokenSource(command.Timeout.Value)
                : new CancellationTokenSource();

            try
            {
                // TODO rename "InvokeAsync" in the log here?
                log.InfoFormat("Invoke Command={0} Breaker={1} Pool={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.PoolKey,
                    command.Timeout.HasValue ? command.Timeout.Value.TotalMilliseconds.ToString() : "Disabled");

                // TODO soon, this comment may not be true
                // Note: this actually awaits the *enqueueing* of the task, not the task execution itself.
                var result = _bulkheadInvoker.ExecuteWithBulkhead(command, cts.Token);
                executeStopwatch.Stop();
                return result;
            }
            catch (Exception e)
            {
                var tokenSourceCancelled = cts.IsCancellationRequested;
                executeStopwatch.Stop();
                var instigator = GetCommandFailedException(e, tokenSourceCancelled, out status).WithData(new
                {
                    Command = command.Name,
                    Timeout = (command.Timeout.HasValue ? command.Timeout.Value.TotalMilliseconds.ToString() : "Disabled"),
                    Status = status,
                    Breaker = command.BreakerKey,
                    Pool = command.PoolKey,
                });

                // We don't log the exception here - that's intentional.

                // If a fallback is not implemented, the exception will get re-thrown and (hopefully) caught
                // and logged by an upstream container. This is the majority of cases, so logging here
                // results in a lot of extra, unnecessary logs and stack traces.

                // If a fallback is implemented, the burden is on the implementation to log or rethrow the
                // exception. Otherwise it'll be eaten. This is documented on the Fallback() method.

                // TODO re-think fallbacks; what of async vs. sync support?
                // - should fallbacks be interface-driven, e.g. AsyncFallback / SyncFallback?

                return TryFallback(instigator);
            }
            finally
            {
                invokeStopwatch.Stop();

                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), executeStopwatch.Elapsed);
                _stats.Elapsed(command.StatsPrefix + " total", status.ToString(), invokeStopwatch.Elapsed);
            }
        }



        private static CommandFailedException GetCommandFailedException(Exception e, bool timeoutTokenTriggered, out CommandCompletionStatus status)
        {
            status = CommandCompletionStatus.Faulted;
            if (IsCancellationException(e))
            {
                // If the timeout cancellationTokenSource was cancelled and we got an TaskCancelledException here then this means the call actually timed out.
                // Otherwise an TaskCancelledException would have been raised if a user CancellationToken was passed through to the method call, and was explicitly
                // cancelled from the client side.
                if (timeoutTokenTriggered)
                {
                    status = CommandCompletionStatus.TimedOut;
                    return new CommandTimeoutException(e);
                }
                status = CommandCompletionStatus.Canceled;
                return new CommandCancelledException(e);
            }

            if (e is CircuitBreakerRejectedException || e is IsolationThreadPoolRejectedException)
            {
                status = CommandCompletionStatus.Rejected;
                return new CommandRejectedException(e);
            }

            return new CommandFailedException(e);
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }
    }
}
