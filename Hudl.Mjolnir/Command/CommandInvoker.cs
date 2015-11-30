using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Bulkhead;
using log4net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// Invokes commands, protecting and isolating the execution with timeouts, circuit breakers,
    /// and bulkheads.
    /// 
    /// <seealso cref="CommandInvoker"/>
    /// </summary>
    public interface ICommandInvoker
    {
        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception.
        /// 
        /// If the command failed, the result will contain the causing exception. If the command
        /// was successful, the result will have a properly set value.
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <returns>A Task wrapping a CommandResult.</returns>
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command);

        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception. The provided timeout will override the timeout defined by
        /// the command's constructor, and will also override any configured timeouts.
        /// 
        /// If the command failed, the result will contain the causing exception. If the command
        /// was successful, the result will have a properly set value.
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="timeoutMillis">
        ///     A timeout that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A Task wrapping a CommandResult.</returns>
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis);

        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception. The provided CancellationToken will override the timeout
        /// defined by the command's constructor, and will also override any configured timeouts.
        /// 
        /// If the command failed, the result will contain the causing exception. If the command
        /// was successful, the result will have a properly set value.
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="ct">
        ///     A cancellation token that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A Task wrapping a CommandResult.</returns>
        Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);

        /// <summary>
        /// Invokes the provided command. If the command fails (due to any exception, be it
        /// Mjolnir's or a fault in the command's execution itself), the exception will be
        /// rethrown.
        /// 
        /// Callers should consider using the <code>InvokeReturn*</code> overloads where possible
        /// to handle failure gracefully (e.g. using fallbacks or retries).
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <returns>A Task wrapping the command's execution result.</returns>
        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command);

        /// <summary>
        /// Invokes the provided command. If the command fails (due to any exception, be it
        /// Mjolnir's or a fault in the command's execution itself), the exception will be
        /// rethrown. The provided timeout will override the timeout defined by the command's
        /// constructor, and will also override any configured timeouts.
        /// 
        /// Callers should consider using the <code>InvokeReturn*</code> overloads where possible
        /// to handle failure gracefully (e.g. using fallbacks or retries).
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="timeoutMillis">
        ///     A timeout that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A Task wrapping the command's execution result.</returns>
        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis);

        /// <summary>
        /// Invokes the provided command. If the command fails (due to any exception, be it
        /// Mjolnir's or a fault in the command's execution itself), the exception will be
        /// rethrown. The provided CancellationToken will override the timeout defined by the
        /// command's constructor, and will also override any configured timeouts.
        /// 
        /// Callers should consider using the <code>InvokeReturn*</code> overloads where possible
        /// to handle failure gracefully (e.g. using fallbacks or retries).
        /// 
        /// <seealso cref="AsyncCommand{TResult}"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="ct">
        ///     A cancellation token that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A Task wrapping the command's execution result.</returns>
        Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct);

        /// <summary>
        /// Invokes the provided command, rethrowing any exceptions that happen during execution.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeReturn{TResult}(SyncCommand{TResult})"/>
        /// <seealso cref="InvokeThrowAsync{TResult}(AsyncCommand{TResult})"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="failureAction">Whether to return or throw on failure.</param>
        /// <returns>The command's execution result.</returns>
        TResult InvokeThrow<TResult>(SyncCommand<TResult> command);

        /// <summary>
        /// Invokes the provided command, rethrowing any exceptions that happen during execution.
        /// The provided timeout will override the timeout defined by the command's constructor,
        /// and will also override any configured timeouts.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeReturn{TResult}(SyncCommand{TResult}, long)"/>
        /// <seealso cref="InvokeThrowAsync{TResult}(AsyncCommand{TResult}, long)"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="timeoutMillis">
        ///     A timeout that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>The command's execution result.</returns>
        TResult InvokeThrow<TResult>(SyncCommand<TResult> command, long timeoutMillis);

        /// <summary>
        /// Invokes the provided command, rethrowing any exceptions that happen during execution.
        /// The provided CancellationToken will override the timeout defined by the command's
        /// constructor, and will also override any configured timeouts.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeReturn{TResult}(SyncCommand{TResult}, CancellationToken)"/>
        /// <seealso cref="InvokeThrowAsync{TResult}(AsyncCommand{TResult}, CancellationToken)"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="ct">
        ///     A cancellation token that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>The command's execution result.</returns>
        TResult InvokeThrow<TResult>(SyncCommand<TResult> command, CancellationToken ct);

        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception.
        /// 
        /// If the command fails, the result will contain the causing exception. If the command
        /// succeeds, the result will have a properly set value.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeThrow{TResult}(SyncCommand{TResult})"/>
        /// <seealso cref="InvokeReturnAsync{TResult}(AsyncCommand{TResult})"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <returns>A CommandResult with a return value or exception information.</returns>
        CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command);

        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception. The provided timeout will override the timeout defined by
        /// the command's constructor, and will also override any configured timeouts.
        /// 
        /// If the command fails, the result will contain the causing exception. If the command 
        /// succeeds, the result will have a properly set value.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeThrow{TResult}(SyncCommand{TResult}, long)"/>
        /// <seealso cref="InvokeReturnAsync{TResult}(AsyncCommand{TResult}, long)"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="timeoutMillis">
        ///     A timeout that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A CommandResult with a return value or exception information.</returns>
        CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command, long timeoutMillis);

        /// <summary>
        /// Invokes the provided command and returns a wrapped result, even if the command's
        /// execution threw an Exception. The provided CancellationToken will override the timeout
        /// defined by the command's constructor, and will also override any configured timeouts.
        /// 
        /// If the command fails, the result will contain the causing exception. If the command
        /// succeeds, the result will have a properly set value.
        /// 
        /// <seealso cref="SyncCommand{TResult}"/>
        /// <seealso cref="CommandResult{TResult}"/>
        /// <seealso cref="InvokeThrow{TResult}(SyncCommand{TResult}, CancellationToken)"/>
        /// <seealso cref="InvokeReturnAsync{TResult}(AsyncCommand{TResult}, CancellationToken)"/>
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by command's execution.</typeparam>
        /// <param name="command">The command to invoke.</param>
        /// <param name="ct">
        ///     A cancellation token that overrides the defined and configured timeouts.
        /// </param>
        /// <returns>A CommandResult with a return value or exception information.</returns>
        CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command, CancellationToken ct);
    }
    
    /// <summary>
    /// Invoker is thread-safe. Prefer to keep a single instance around and use it throughout your
    /// application (e.g. via dependency injection).
    /// </summary>
    public class CommandInvoker : ICommandInvoker
    {
        private readonly ICommandContext _context;
        private readonly IBulkheadInvoker _bulkheadInvoker;

        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the
        /// default timeout. This is likely to be useful when debugging Command-decorated methods,
        /// however it is not advisable to use in a production environment since it disables some
        /// of Mjolnir's key protection features.
        /// </summary>
        private readonly IConfigurableValue<bool> _ignoreCancellation;

        /// <summary>
        /// Singleton instance. Prefer to inject ICommandInvoker into constructors where possible.
        /// This can be used in older code where it's not as easy to introduce things like DI.
        /// </summary>
        public static readonly ICommandInvoker Instance = new CommandInvoker();

        public CommandInvoker() : this(null, null, null)
        { }
        
        internal CommandInvoker(ICommandContext context = null, IBulkheadInvoker bulkheadInvoker = null, IConfigurableValue<bool> ignoreTimeouts = null)
        {
            _context = context ?? CommandContext.Current;

            var breakerInvoker = new BreakerInvoker(_context);
            _bulkheadInvoker = bulkheadInvoker ?? new BulkheadInvoker(breakerInvoker, _context);

            _ignoreCancellation = ignoreTimeouts ?? new ConfigurableValue<bool>("mjolnir.ignoreTimeouts", false);
        }
        
        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command);
            return InvokeAsync(command, token, OnFailure.Throw);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAsync(command, token, OnFailure.Throw);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(ct);
            return InvokeAsync(command, token, OnFailure.Throw);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command)
        {
            var token = GetCancellationTokenForCommand(command);
            return InvokeAndWrapAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis)
        {
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAndWrapAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            var token = GetCancellationTokenForCommand(ct);
            return InvokeAndWrapAsync(command, token);
        }

        private async Task<CommandResult<TResult>> InvokeAndWrapAsync<TResult>(AsyncCommand<TResult> command, InformativeCancellationToken ct)
        {
            // Even though we're in a "Return" method, multiple invokes are a bug on the calling
            // side, hence the possible exception here for visibility so the caller can fix.
            EnsureSingleInvoke(command);

            try
            {
                var result = await InvokeAsync(command, ct, OnFailure.Return);
                return new CommandResult<TResult>(result);
            }
            catch (Exception e)
            {
                return new CommandResult<TResult>(default(TResult), e);
            }
        }

        // failureModeForMetrics is just so we can send "throw" or "return" along with the metrics
        // event we fire for CommandInvoke(). Not really intended for use beyond that.
        private async Task<TResult> InvokeAsync<TResult>(AsyncCommand<TResult> command, InformativeCancellationToken ct, OnFailure failureModeForMetrics)
        {
            var stopwatch = Stopwatch.StartNew();

            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var status = CommandCompletionStatus.RanToCompletion;

            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Bulkhead={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    ct.DescriptionForLog);

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                return await _bulkheadInvoker.ExecuteWithBulkheadAsync(command, ct.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                status = GetCompletionStatus(e, ct);
                AttachCommandExceptionData(command, e, status, ct);
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _context.MetricEvents.CommandInvoked(command.Name, stopwatch.Elapsed.TotalMilliseconds, command.ExecutionTimeMillis, status.ToString(), failureModeForMetrics.ToString().ToLowerInvariant());
            }
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command, long timeoutMillis)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            EnsureSingleInvoke(command);

            var token = GetCancellationTokenForCommand(ct);
            return Invoke(command, OnFailure.Throw, token);
        }

        public CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command)
        {
            var token = GetCancellationTokenForCommand(command);
            return InvokeAndWrap(command, token);
        }

        public CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command, long timeoutMillis)
        {
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAndWrap(command, token);
        }

        public CommandResult<TResult> InvokeReturn<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            var token = GetCancellationTokenForCommand(ct);
            return InvokeAndWrap(command, token);
        }

        private CommandResult<TResult> InvokeAndWrap<TResult>(SyncCommand<TResult> command, InformativeCancellationToken ct)
        {
            // Even though we're in a "Return" method, multiple invokes are a bug on the calling
            // side, hence the possible exception here for visibility so the caller can fix.
            EnsureSingleInvoke(command);

            try
            {
                var result = Invoke(command, OnFailure.Return, ct);
                return new CommandResult<TResult>(result);
            }
            catch (Exception e)
            {
                return new CommandResult<TResult>(default(TResult), e);
            }
        }

        private TResult Invoke<TResult>(SyncCommand<TResult> command, OnFailure failureAction, InformativeCancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);
            var status = CommandCompletionStatus.RanToCompletion;
            
            try
            {
                log.InfoFormat("Invoke Command={0} Breaker={1} Bulkhead={2} Timeout={3}",
                    command.Name,
                    command.BreakerKey,
                    command.BulkheadKey,
                    ct.DescriptionForLog);

                // If we've already timed out or been canceled, skip execution altogether.
                ct.Token.ThrowIfCancellationRequested();

                return _bulkheadInvoker.ExecuteWithBulkhead(command, ct.Token);
            }
            catch (Exception e)
            {
                status = GetCompletionStatus(e, ct);
                AttachCommandExceptionData(command, e, status, ct);
                throw;                
            }
            finally
            {
                stopwatch.Stop();
                _context.MetricEvents.CommandInvoked(command.Name, stopwatch.Elapsed.TotalMilliseconds, command.ExecutionTimeMillis, status.ToString(), failureAction.ToString().ToLowerInvariant());
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
            if (Interlocked.CompareExchange(ref command.HasInvoked, 1, 0) > 0)
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

        private static void AttachCommandExceptionData(BaseCommand command, Exception exception, CommandCompletionStatus status, InformativeCancellationToken ct)
        {
            exception.WithData(new
            {
                Command = command.Name,
                Status = status,
                Breaker = command.BreakerKey,
                Bulkhead = command.BulkheadKey,
                TimeoutMillis = ct.DescriptionForLog,
                ExecuteMillis = command.ExecutionTimeMillis,
            });
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }
    }

    // "Failure" means any of [Fault || Timeout || Reject]
    internal enum OnFailure
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
