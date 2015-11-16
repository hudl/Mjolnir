using Hudl.Common.Extensions;
using Hudl.Config;
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
        Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction);
        Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, long timeoutMillis);
        Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, CancellationToken ct);

        CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction);
        CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, long timeoutMillis);
        CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, CancellationToken ct);

        // TODO possible alternate signatures:
        // 
        // Could forego the CommandResult wrapper, which would enforce a OnFailure.Throw
        //   TResult InvokeAndUnwrapOrThrow<TResult>(...)
    }

    // TODO what do timeouts/cancellations look like in exceptions now? make sure we didn't revert that logging change

    public class CommandInvoker : ICommandInvoker
    {
        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the
        /// default timeout. This is likely to be useful when debugging Command-decorated methods,
        /// however it is not advisable to use in a production environment since it disables some
        /// of Mjolnir's key protection features.
        /// </summary>
        private static readonly IConfigurableValue<bool> IgnoreCommandTimeouts = new ConfigurableValue<bool>("mjolnir.ignoreTimeouts", false);

        private readonly IStats _stats;

        // TODO kind of ugly, rework this. they're lightweight to construct, though, and
        // callers shouldn't be repeatedly constructing invokers.
        // TODO make sure callers know not to repeatedly construct invokers :)
        private readonly IBulkheadInvoker _bulkheadInvoker = new BulkheadInvoker(new BreakerInvoker());

        public CommandInvoker() : this(CommandContext.Stats)
        {
            _stats = CommandContext.Stats; // TODO any risk here? should we just DI this? possibly not.
        }

        internal CommandInvoker(IStats stats, IBulkheadInvoker bulkheadInvoker = null)
        {
            if (stats == null)
            {
                throw new ArgumentNullException("stats");
            }

            _stats = stats;
            if (bulkheadInvoker != null)
            {
                _bulkheadInvoker = bulkheadInvoker;
            }
        }

        public Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction)
        {
            var token = GetCancellationTokenForCommand(command);
            return InvokeAsync(command, failureAction, token);
        }

        public Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, long timeoutMillis)
        {
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAsync(command, failureAction, token);
        }

        // TODO doesn't protect against None/default tokens. Should it?
        public Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, CancellationToken ct)
        {
            var informative = InformativeCancellationToken.ForCancellationToken(ct);
            return InvokeAsync(command, failureAction, informative);
        }

        // TODO have the command naming logic remove "AsyncCommand" suffixes as well

        private async Task<CommandResult<TResult>> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureAction, InformativeCancellationToken ct)
        {
            // This doesn't adhere to the OnFailure action because it's a bug in the code
            // and should always throw so people see it and fix it.
            EnsureSingleInvoke(command);
            
            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var status = CommandCompletionStatus.RanToCompletion;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Bulkhead={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    GetTimeoutForLog(ct.Timeout));

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                var result = await _bulkheadInvoker.ExecuteWithBulkheadAsync(command, ct.Token).ConfigureAwait(false);
                stopwatch.Stop();

                return new CommandResult<TResult>(result, status);
            }
            catch (Exception e)
            {
                stopwatch.Stop();

                status = GetCompletionStatus(e, ct);
                AttachCommandExceptionData(command, e, status, ct, stopwatch);

                if (failureAction == OnFailure.Throw)
                {
                    throw;
                }

                return new CommandResult<TResult>(default(TResult), status, e);
            }
            finally
            {
                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), stopwatch.Elapsed);
            }
        }

        public CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction)
        {
            var token = GetCancellationTokenForCommand(command);
            return Invoke(command, failureAction, token);
        }

        public CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, long timeoutMillis)
        {
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return Invoke(command, failureAction, token);
        }

        // TODO doesn't protect against None/default tokens. Should it?
        public CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, CancellationToken ct)
        {
            var informative = InformativeCancellationToken.ForCancellationToken(ct);
            return Invoke(command, failureAction, informative);
        }

        private CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, InformativeCancellationToken ct)
        {
            // This doesn't adhere to the OnFailure action because it's a bug in the code
            // and should always throw so people see it and fix it.
            EnsureSingleInvoke(command);
            
            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var status = CommandCompletionStatus.RanToCompletion;
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Bulkhead={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    GetTimeoutForLog(ct.Timeout));

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                var result = _bulkheadInvoker.ExecuteWithBulkhead(command, ct.Token);
                stopwatch.Stop();

                return new CommandResult<TResult>(result, status);
            }
            catch (Exception e)
            {
                stopwatch.Stop();

                status = GetCompletionStatus(e, ct);
                AttachCommandExceptionData(command, e, status, ct, stopwatch);

                if (failureAction == OnFailure.Throw)
                {
                    throw;
                }

                return new CommandResult<TResult>(default(TResult), status, e);
            }
            finally
            {
                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), stopwatch.Elapsed);
            }
        }

        private static InformativeCancellationToken GetCancellationTokenForCommand(BaseCommand command, long? invocationTimeout = null)
        {
            if (IgnoreCommandTimeouts.Value)
            {
                return InformativeCancellationToken.ForCancellationToken(CancellationToken.None);
            }

            var timeout = command.DetermineTimeout(invocationTimeout);
            return InformativeCancellationToken.ForTimeout(timeout);
        }

        private static void EnsureSingleInvoke(BaseCommand command)
        {
            if (Interlocked.CompareExchange(ref command._hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }
        }
        
        private static CommandCompletionStatus GetCompletionStatus(Exception exception, InformativeCancellationToken ct)
        {
            if (IsCancellationException(exception))
            {
                // If the timeout cancellationTokenSource was cancelled and we got an TaskCancelledException here then this means the call actually timed out.
                // Otherwise an TaskCancelledException would have been raised if a user CancellationToken was passed through to the method call, and was explicitly
                // cancelled from the client side.
                if (ct.IsTimeoutToken && ct.Token.IsCancellationRequested)
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

        private static void AttachCommandExceptionData(BaseCommand command, Exception exception, CommandCompletionStatus status, InformativeCancellationToken ct, Stopwatch invokeTimer)
        {
            exception.WithData(new
            {
                Command = command.Name,
                Status = status,
                Breaker = command.BreakerKey,
                Bulkhead = command.BulkheadKey,
                TimeoutMillis = GetTimeoutForLog(ct.Timeout),
                ElapsedMillis = invokeTimer.Elapsed.TotalMilliseconds,
            });
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }

        private static object GetTimeoutForLog(TimeSpan? timeout)
        {
            return (timeout.HasValue ? (int) timeout.Value.TotalMilliseconds : (object) "Disabled");
        }
    }

    // "Failure" is any of [Fault || Timeout || Reject]
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

    // Keeps track of how a CancellationToken was formed, where possible.
    // This is mostly for diagnostic purposes and logging.
    internal struct InformativeCancellationToken
    {
        private readonly CancellationToken _token;
        private readonly TimeSpan? _timeout;

        public CancellationToken Token { get { return _token; } }
        public bool IsTimeoutToken { get { return _timeout != null; } }
        public TimeSpan? Timeout { get { return _timeout; } }

        private InformativeCancellationToken(CancellationToken token)
        {
            _token = token;
            _timeout = null;
        }

        private InformativeCancellationToken(TimeSpan timeout)
        {
            var source = new CancellationTokenSource(timeout);
            _token = source.Token;
            _timeout = timeout;
        }
        
        public static InformativeCancellationToken ForTimeout(long millis)
        {
            var timespan = TimeSpan.FromMilliseconds(millis);
            return new InformativeCancellationToken(timespan);
        }

        public static InformativeCancellationToken ForTimeout(TimeSpan timeout)
        {
            return new InformativeCancellationToken(timeout);
        }

        public static InformativeCancellationToken ForCancellationToken(CancellationToken ct)
        {
            return new InformativeCancellationToken(ct);
        }
    }
}
