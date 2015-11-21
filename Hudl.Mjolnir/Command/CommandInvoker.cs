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
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command);
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis);
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);

        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command);
        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis);
        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);

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
        private readonly IStats _stats;
        
        private readonly IBulkheadInvoker _bulkheadInvoker;

        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the
        /// default timeout. This is likely to be useful when debugging Command-decorated methods,
        /// however it is not advisable to use in a production environment since it disables some
        /// of Mjolnir's key protection features.
        /// </summary>
        private readonly IConfigurableValue<bool> _ignoreCancellation;

        public CommandInvoker() : this(null, null, null)
        { }

        internal CommandInvoker(IStats stats = null, IBulkheadInvoker bulkheadInvoker = null, IConfigurableValue<bool> ignoreTimeouts = null)
        {
            _stats = stats ?? CommandContext.Stats;
            
            // TODO kind of ugly, rework this. they're lightweight to construct, though, and
            // callers shouldn't be repeatedly constructing invokers.
            // TODO make sure callers know not to repeatedly construct invokers :)
            _bulkheadInvoker = bulkheadInvoker ?? new BulkheadInvoker(new BreakerInvoker());

            _ignoreCancellation = ignoreTimeouts ?? new ConfigurableValue<bool>("mjolnir.ignoreTimeouts", false);
        }

        // TODO have the command naming logic remove "AsyncCommand" suffixes as well

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command);
            return InvokeAsync(command, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAsync(command, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(ct);
            return InvokeAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command)
        {
            var token = GetCancellationTokenForCommand(command);
            return InvokeReturnAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis)
        {
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeReturnAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            var token = GetCancellationTokenForCommand(ct);
            return InvokeReturnAsync(command, token);
        }

        private async Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, InformativeCancellationToken ct)
        {
            // This doesn't adhere to the OnFailure action because it's a bug in the code
            // and should always throw so people see it and fix it.
            EnsureSingleInvoke(command);

            try
            {
                var result = await InvokeAsync(command, ct);
                return new CommandResult<TResult>(result);
            }
            catch (Exception e)
            {
                return new CommandResult<TResult>(default(TResult), e);
            }
        }

        private async Task<TResult> InvokeAsync<TResult>(AsyncCommand<TResult> command, InformativeCancellationToken ct)
        {
            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var status = CommandCompletionStatus.RanToCompletion;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Bulkhead={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    ct.DescriptionForLog);

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                var result = await _bulkheadInvoker.ExecuteWithBulkheadAsync(command, ct.Token).ConfigureAwait(false);
                stopwatch.Stop();

                return result;
            }
            catch (Exception e)
            {
                stopwatch.Stop();

                status = GetCompletionStatus(e, ct);
                AttachCommandExceptionData(command, e, status, ct, stopwatch);

                throw;
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

        public CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, CancellationToken ct)
        {
            var token = GetCancellationTokenForCommand(ct);
            return Invoke(command, failureAction, token);
        }

        private CommandResult<TResult> Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, InformativeCancellationToken ct)
        {
            // This doesn't look at the OnFailure action (to return vs. throw) because it means the
            // caller has a bug in their code - we should always throw so people see it and fix it.
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
                    ct.DescriptionForLog);

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                var result = _bulkheadInvoker.ExecuteWithBulkhead(command, ct.Token);
                stopwatch.Stop();

                return new CommandResult<TResult>(result);
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

                return new CommandResult<TResult>(default(TResult), e);
            }
            finally
            {
                _stats.Elapsed(command.StatsPrefix + " execute", status.ToString(), stopwatch.Elapsed);
            }
        }

        private InformativeCancellationToken GetCancellationTokenForCommand(CancellationToken ct)
        {
            if (_ignoreCancellation.Value)
            {
                return InformativeCancellationToken.ForIgnored();
            }

            return InformativeCancellationToken.ForOverridingToken(ct);
        }

        private InformativeCancellationToken GetCancellationTokenForCommand(BaseCommand command, long? invocationTimeout = null)
        {
            if (_ignoreCancellation.Value)
            {
                return InformativeCancellationToken.ForIgnored();
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
                if (ct.Timeout.HasValue && ct.Token.IsCancellationRequested)
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
                TimeoutMillis = ct.DescriptionForLog,
                ElapsedMillis = invokeTimer.Elapsed.TotalMilliseconds,
            });
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
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
        private readonly Exception _exception;

        public TResult Value { get { return _value; } }
        public Exception Exception { get { return _exception; } }
        public bool WasSuccess { get { return _exception == null; } }

        internal CommandResult(TResult value, Exception exception = null)
        {
            _value = value;
            _exception = exception;
        }
    }

    /// <summary>
    /// Mostly for diagnostic purposes and logging, and should only be used internally. It's often
    /// helpful to know the source of cancellation; when throwing an exception, we can be specific
    /// in the message (e.g. "timed out" vs. just "canceled", which is clearer to the caller).
    /// </summary>
    internal struct InformativeCancellationToken
    {
        // Cancellation precedence
        // - Config toggle to disable timeouts/cancellation
        // - Cancellation token or timeout passed to Invoke()
        // - Configured timeout for specific Command
        // - Default timeout in Command constructor

        private readonly CancellationToken _token;
        private readonly TimeSpan? _timeout;
        private readonly bool _isIgnored;

        public CancellationToken Token { get { return _token; } }
        public TimeSpan? Timeout { get { return _timeout; } }
        public bool IsIgnored {  get { return _isIgnored; } }
        
        private InformativeCancellationToken(CancellationToken token, bool ignored = false)
        {
            _timeout = null;
            _isIgnored = ignored;
            _token = token;
        }

        private InformativeCancellationToken(TimeSpan timeout)
        {
            _timeout = timeout;
            _isIgnored = false;

            // This (int) cast probably breaks timeouts that are < 1 millisecond. I'm not sure
            // that's worth fixing yet. Notable, though.
            if ((int) timeout.TotalMilliseconds == 0)
            {
                // If timeout is 0, we're immediately timed-out. This is somewhat
                // here as a convenience for unit testing, but applies generally.
                // Unit tests may use a 0ms timeout for some tests, and if tests
                // execute too quickly, the cancellation token's internal timer
                // may not have marked the token canceled yet. This works around
                // that.

                // Note that 0 is not "infinite" - that's typically the behavior
                // for a timeout of -1.

                _token = new CancellationToken(true);
            }
            else
            {
                var source = new CancellationTokenSource(timeout);
                _token = source.Token;
            }
        }
        
        public object DescriptionForLog
        {
            get
            {
                if (_isIgnored)
                {
                    return "Ignored";
                }

                if (_timeout.HasValue)
                {
                    return (int) _timeout.Value.TotalMilliseconds;
                }

                if (_token == CancellationToken.None || _token == default(CancellationToken))
                {
                    return "None";
                }
                
                return "Token";
            }
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

        public static InformativeCancellationToken ForOverridingToken(CancellationToken ct)
        {
            return new InformativeCancellationToken(ct, false);
        }

        public static InformativeCancellationToken ForIgnored()
        {
            return new InformativeCancellationToken(CancellationToken.None, true);
        }
    }
}
