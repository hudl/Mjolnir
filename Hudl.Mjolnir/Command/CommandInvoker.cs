using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Bulkhead;
using Hudl.Mjolnir.Config;
using Hudl.Mjolnir.Events;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Log;
using System;
using System.Collections.Generic;
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
        ///     A timeout that overrides the defined and configured timeouts. Use this only when
        ///     necessary, and prefer to tune timeouts with configurable values instead of
        ///     these hard-coded, per-call timeouts.
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
        ///     A timeout that overrides the defined and configured timeouts. Use this only when
        ///     necessary, and prefer to tune timeouts with configurable values instead of
        ///     these hard-coded, per-call timeouts.
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
        ///     A timeout that overrides the defined and configured timeouts. Use this only when
        ///     necessary, and prefer to tune timeouts with configurable values instead of
        ///     these hard-coded, per-call timeouts.
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
        ///     A timeout that overrides the defined and configured timeouts. Use this only when
        ///     necessary, and prefer to tune timeouts with configurable values instead of
        ///     these hard-coded, per-call timeouts.
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
        private readonly IMjolnirConfig _config;
        private readonly IMjolnirLogFactory _logFactory;
        
        private readonly IMetricEvents _metricEvents;
        private readonly IBreakerExceptionHandler _breakerExceptionHandler;
        private readonly IBulkheadInvoker _bulkheadInvoker;

        private readonly ICircuitBreakerFactory _circuitBreakerFactory;
        private readonly IBulkheadFactory _bulkheadFactory;

        public CommandInvoker()
            : this(new DefaultValueConfig(), new DefaultMjolnirLogFactory(), new IgnoringMetricEvents(), null, null)
        { }
        
        public CommandInvoker(
            IMjolnirConfig config = null,
            IMjolnirLogFactory logFactory = null,
            IMetricEvents metricEvents = null,
            IBreakerExceptionHandler breakerExceptionHandler = null)
            : this(config, logFactory, metricEvents, breakerExceptionHandler, null)
        { }
        
        // Internal constructor with a few extra arguments used by tests to inject mocks.
        internal CommandInvoker(
            IMjolnirConfig config = null,
            IMjolnirLogFactory logFactory = null,
            IMetricEvents metricEvents = null,
            IBreakerExceptionHandler breakerExceptionHandler = null,
            IBulkheadInvoker bulkheadInvoker = null)
        {
            _config = config ?? new DefaultValueConfig();
            _logFactory = logFactory ?? new DefaultMjolnirLogFactory();
            _metricEvents = metricEvents ?? new IgnoringMetricEvents();
            _breakerExceptionHandler = breakerExceptionHandler ?? new IgnoredExceptionHandler(new HashSet<Type>());
            
            _circuitBreakerFactory = new CircuitBreakerFactory(
                _metricEvents,
                new FailurePercentageCircuitBreakerConfig(_config),
                _logFactory);

            _bulkheadFactory = new BulkheadFactory(
                _metricEvents,
                new BulkheadConfig(_config),
                _logFactory);

            var breakerInvoker = new BreakerInvoker(_circuitBreakerFactory, _metricEvents, _breakerExceptionHandler);
            _bulkheadInvoker = bulkheadInvoker ?? new BulkheadInvoker(breakerInvoker, _bulkheadFactory, _metricEvents, _config);
        }
        
        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command);
            return InvokeAsync(command, OnFailure.Throw, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, long timeoutMillis)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAsync(command, OnFailure.Throw, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(ct);
            return InvokeAsync(command, OnFailure.Throw, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command);
            return InvokeAsync(command, OnFailure.Throw, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func, long timeoutMillis)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAsync(command, OnFailure.Throw, token);
        }

        public Task<TResult> InvokeThrowAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func, CancellationToken ct)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.ExecuteAsync(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(ct);
            return InvokeAsync(command, OnFailure.Throw, token);
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

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);
            var token = GetCancellationTokenForCommand(command);
            return InvokeAndWrapAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func, long timeoutMillis)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAndWrapAsync(command, token);
        }

        public Task<CommandResult<TResult>> InvokeReturnAsync<TResult>(string group, Func<CancellationToken?, Task<TResult>> func, CancellationToken ct)
        {
            var command = new DelegateAsyncCommand<TResult>(group, func);
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
                TResult result;
                if (!IsEnabled())
                {
                    result = await command.ExecuteAsync(CancellationToken.None);
                }
                else
                {
                    result = await InvokeAsync(command, OnFailure.Return, ct);
                }
                return new CommandResult<TResult>(result);
            }
            catch (Exception e)
            {
                return new CommandResult<TResult>(default(TResult), e);
            }
        }

        // failureModeForMetrics is just so we can send "throw" or "return" along with the metrics
        // event we fire for CommandInvoke(). Not really intended for use beyond that.
        private async Task<TResult> InvokeAsync<TResult>(AsyncCommand<TResult> command, OnFailure failureModeForMetrics, InformativeCancellationToken ct)
        {
            var stopwatch = Stopwatch.StartNew();

            var logName = $"Hudl.Mjolnir.Command.{command.Name}";
            var log = _logFactory.CreateLog(logName);
            if (log == null)
            {
                throw new InvalidOperationException($"{nameof(IMjolnirLogFactory)} implementation returned null from {nameof(IMjolnirLogFactory.CreateLog)} for name {logName}, please make sure the implementation returns a non-null log for all calls to {nameof(IMjolnirLogFactory.CreateLog)}");
            }

            var status = CommandCompletionStatus.RanToCompletion;

            try
            {
                log.Info($"Invoke Command={command.Name} Breaker={command.BreakerKey} Bulkhead={command.BulkheadKey} Timeout={ct.DescriptionForLog}");

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
                _metricEvents.CommandInvoked(command.Name, stopwatch.Elapsed.TotalMilliseconds, command.ExecutionTimeMillis, status.ToString(), failureModeForMetrics.ToString().ToLowerInvariant());
            }
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command, long timeoutMillis)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            EnsureSingleInvoke(command);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(ct);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(string group, Func<CancellationToken?, TResult> func)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(string group, Func<CancellationToken?, TResult> func, long timeoutMillis)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return Invoke(command, OnFailure.Throw, token);
        }

        public TResult InvokeThrow<TResult>(string group, Func<CancellationToken?, TResult> func, CancellationToken ct)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);

            if (!IsEnabled())
            {
                return command.Execute(CancellationToken.None);
            }

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

        public CommandResult<TResult> InvokeReturn<TResult>(string group, Func<CancellationToken?, TResult> func)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);
            var token = GetCancellationTokenForCommand(command);
            return InvokeAndWrap(command, token);
        }

        public CommandResult<TResult> InvokeReturn<TResult>(string group, Func<CancellationToken?, TResult> func, long timeoutMillis)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);
            var token = GetCancellationTokenForCommand(command, timeoutMillis);
            return InvokeAndWrap(command, token);
        }

        public CommandResult<TResult> InvokeReturn<TResult>(string group, Func<CancellationToken?, TResult> func, CancellationToken ct)
        {
            var command = new DelegateSyncCommand<TResult>(group, func);
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
                TResult result;
                if (!IsEnabled())
                {
                    result = command.Execute(CancellationToken.None);
                }
                else
                {
                    result = Invoke(command, OnFailure.Return, ct);
                }
                
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

            var logName = $"Hudl.Mjolnir.Command.{command.Name}";
            var log = _logFactory.CreateLog(logName);
            if (log == null)
            {
                throw new InvalidOperationException($"{nameof(IMjolnirLogFactory)} implementation returned null from {nameof(IMjolnirLogFactory.CreateLog)} for name {logName}, please make sure the implementation returns a non-null log for all calls to {nameof(IMjolnirLogFactory.CreateLog)}");
            }

            var status = CommandCompletionStatus.RanToCompletion;
            
            try
            {
                log.Info($"Invoke Command={command.Name} Breaker={command.BreakerKey} Bulkhead={command.BulkheadKey} Timeout={ct.DescriptionForLog}");

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
                _metricEvents.CommandInvoked(command.Name, stopwatch.Elapsed.TotalMilliseconds, command.ExecutionTimeMillis, status.ToString(), failureAction.ToString().ToLowerInvariant());
            }
        }

        private InformativeCancellationToken GetCancellationTokenForCommand(CancellationToken ct)
        {
            if (IgnoreCancellation())
            {
                return InformativeCancellationToken.ForIgnored();
            }

            if (!IsEnabled())
            {
                return InformativeCancellationToken.ForDisabled();
            }

            return InformativeCancellationToken.ForOverridingToken(ct);
        }

        private InformativeCancellationToken GetCancellationTokenForCommand(BaseCommand command, long? invocationTimeout = null)
        {
            if (IgnoreCancellation())
            {
                return InformativeCancellationToken.ForIgnored();
            }

            var timeout = command.DetermineTimeout(_config, invocationTimeout);
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
            // Use namespaced keys here to avoid clobbering data that the application might've
            // already attached (or will attach later).
            exception.Data["MjolnirCommand"] = command.Name;
            exception.Data["MjolnirStatus"] = status;
            exception.Data["MjolnirBreaker"] = command.BreakerKey;
            exception.Data["MjolnirBulkhead"] = command.BulkheadKey;
            exception.Data["MjolnirTimeoutMillis"] = ct.DescriptionForLog;
            exception.Data["MjolnirExecuteMillis"] = command.ExecutionTimeMillis;
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }

        /// <summary>
        /// Global killswitch for Mjolnir. If configured to <code>false</code>, Mjolnir will still
        /// do some initial work (like ensuring a single invoke per Command), but will then just
        /// execute the Command instead of passing it through Bulkheads and Circuit Breakers.
        /// No timeouts will be applied; a CancellationToken.None will be passed to any method
        /// that supports cancellation.
        /// </summary>
        private bool IsEnabled()
        {
            return _config.GetConfig("mjolnir.isEnabled", true);
        }
        
        /// <summary>
        /// If this is set to true then all calls wrapped in a Mjolnir command will ignore the
        /// default timeout. This is likely to be useful when debugging Command-decorated methods,
        /// however it is not advisable to use in a production environment since it disables some
        /// of Mjolnir's key protection features.
        /// </summary>
        private bool IgnoreCancellation()
        {
            return _config.GetConfig("mjolnir.ignoreTimeouts", false);
        }

        // "Failure" means any of [Fault || Timeout || Reject]
        private enum OnFailure
        {
            Throw,
            Return,
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

        public static InformativeCancellationToken ForDisabled()
        {
            return new InformativeCancellationToken(CancellationToken.None, false);
        }
    }
}
