using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.ThreadPool;
using log4net;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    public abstract class BaseCommand : Command
    {
        internal readonly TimeSpan? Timeout;
        
        internal readonly bool TimeoutsIgnored;

        private readonly GroupKey _group;
        private readonly string _name;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _poolKey;
        
        // Setters should be used for testing only.

        //private IStats _stats;
        //internal IStats Stats
        //{
        //    internal get { return _stats ?? CommandContext.Stats; }
        //    set { _stats = value; }
        //}

        //private ICircuitBreaker _breaker;
        //internal ICircuitBreaker CircuitBreaker
        //{
        //    internal get { return _breaker ?? CommandContext.GetCircuitBreaker(_breakerKey); }
        //    set { _breaker = value; }
        //}

        //private IIsolationThreadPool _pool;
        //internal IIsolationThreadPool ThreadPool
        //{
        //    internal get { return _pool ?? CommandContext.GetThreadPool(_poolKey); }
        //    set { _pool = value; }
        //}

        //private IIsolationSemaphore _fallbackSemaphore;
        //internal IIsolationSemaphore FallbackSemaphore
        //{
        //    // TODO Consider isolating these per-command instead of per-pool.
        //    internal get { return _fallbackSemaphore ?? CommandContext.GetFallbackSemaphore(_poolKey); }
        //    set { _fallbackSemaphore = value; }
        //}

        // 0 == not yet invoked, > 0 == invoked
        internal int _hasInvoked = 0;

        /// <summary>
        /// Constructs the Command.
        /// 
        /// The group is used as part of the Command's <see cref="Name">Name</see>.
        /// If the group contains dots, they'll be converted to dashes.
        /// 
        /// The provided <code>isolationKey</code> will be used as both the
        /// breaker and pool keys.
        /// 
        /// Command timeouts can be configured at runtime. Configuration keys
        /// follow the form: <code>mjolnir.group-key.CommandClassName.Timeout</code>
        /// (i.e. <code>mjolnir.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="isolationKey">Breaker and pool key to use.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
        protected BaseCommand(string group, string isolationKey, TimeSpan defaultTimeout)
            : this(group, null, isolationKey, isolationKey, defaultTimeout)
        { }

        /// <summary>
        /// Constructs the Command.
        /// 
        /// The group is used as part of the Command's <see cref="Name">Name</see>.
        /// If the group contains dots, they'll be converted to dashes.
        /// 
        /// Command timeouts can be configured at runtime. Configuration keys
        /// follow the form: <code>mjolnir.group-key.CommandClassName.Timeout</code>
        /// (i.e. <code>mjolnir.[Command.Name].Timeout</code>). If not
        /// configured, the provided <code>defaultTimeout</code> will be used.
        /// 
        /// </summary>
        /// <param name="group">Logical grouping for the command, usually the owning team. Avoid using dots.</param>
        /// <param name="breakerKey">Breaker to use for this command.</param>
        /// <param name="poolKey">Pool to use for this command.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
        protected BaseCommand(string group, string breakerKey, string poolKey, TimeSpan defaultTimeout)
            : this(group, null, breakerKey, poolKey, defaultTimeout)
        { }

        internal BaseCommand(string group, string name, string breakerKey, string poolKey, TimeSpan defaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentNullException("group");
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentNullException("breakerKey");
            }

            if (string.IsNullOrWhiteSpace(poolKey))
            {
                throw new ArgumentNullException("poolKey");
            }

            if (defaultTimeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("Positive default timeout is required", "defaultTimeout");
            }

            _group = GroupKey.Named(group);
            _name = string.IsNullOrWhiteSpace(name) ? GenerateAndCacheName(Group) : CacheProvidedName(Group, name);
            _breakerKey = GroupKey.Named(breakerKey);
            _poolKey = GroupKey.Named(poolKey);
            
            Timeout = GetCommandTimeout(defaultTimeout);
        }

        private TimeSpan? GetCommandTimeout(TimeSpan defaultTimeout)
        {
            if (IgnoreCommandTimeouts.Value)
            {
                // TODO log?
                return null;
            }

            var timeout = GetTimeoutConfigurableValue(_name).Value;
            if (timeout <= 0)
            {
                timeout = (long) defaultTimeout.TotalMilliseconds;
            }
            else
            {
                // TODO log?
                //_log.DebugFormat("Timeout configuration override for this command of {0}", timeout);
            }

            return TimeSpan.FromMilliseconds(timeout);
        }

        private string CacheProvidedName(GroupKey group, string name)
        {
            var cacheKey = new Tuple<string, GroupKey>(name, group);
            return ProvidedNameCache.GetOrAdd(cacheKey, t => cacheKey.Item2.Name.Replace(".", "-") + "." + name.Replace(".", "-"));
        }

        // Since creating the Command's name is non-trivial, we'll keep a local
        // cache of them.
        private string GenerateAndCacheName(GroupKey group)
        {
            var type = GetType();
            var cacheKey = new Tuple<Type, GroupKey>(type, group);
            return GeneratedNameCache.GetOrAdd(cacheKey, t =>
            {
                var className = cacheKey.Item1.Name;
                if (className.EndsWith("Command", StringComparison.InvariantCulture))
                {
                    className = className.Substring(0, className.LastIndexOf("Command", StringComparison.InvariantCulture));
                }

                return cacheKey.Item2.Name.Replace(".", "-") + "." + className;
            });
        }

        private static IConfigurableValue<long> GetTimeoutConfigurableValue(string commandName)
        {
            return TimeoutConfigCache.GetOrAdd(commandName, n => new ConfigurableValue<long>("command." + commandName + ".Timeout"));
        }

        internal string Name
        {
            get { return _name; }
        }

        internal GroupKey Group
        {
            get { return _group; }
        }

        internal GroupKey BreakerKey
        {
            get { return _breakerKey; }
        }

        internal GroupKey PoolKey
        {
            get { return _poolKey; }
        }

        internal string StatsPrefix
        {
            get { return "mjolnir command " + Name; }
        }
    }
    
    public sealed class Void
    {
        
    }

    public abstract class AsyncCommand<TResult> : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }
    
    public abstract class SyncCommand<TResult> : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract TResult Execute(CancellationToken cancellationToken);
    }

    public abstract class SyncCommand : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected internal abstract void Execute(CancellationToken cancellationToken);
    }

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
        protected static readonly ConfigurableValue<bool> UseCircuitBreakers = new ConfigurableValue<bool>("mjolnir.useCircuitBreakers", true);

        private readonly IStats _stats;

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
                var result = await ExecuteWithBulkheadAsync(command, cts.Token).ConfigureAwait(false);
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
                var result = ExecuteWithBulkhead(command, cts.Token);
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
        
        private async Task<TResult> ExecuteWithBulkheadAsync<TResult>(AsyncCommand<TResult> command, CancellationToken ct)
        {
            // REWRITE:
            // - Get the semaphore bulkhead for the command group
            // - Reject or increment accordingly.

            // TODO get bulkhead and check; reject if necessary
            try
            {
                // TODO increment bulkhead
                return UseCircuitBreakers.Value
                    ? await ExecuteWithBreakerAsync(command, ct)
                    : await command.ExecuteAsync(ct);
            }
            catch(Exception e)
            {
                // TODO decrement bulkhead
                throw;
            }
            
            // Note: Thread pool rejections shouldn't count as failures to the breaker.
            // If a downstream dependency is slow, the pool will fill up, but the
            // breaker + timeouts will already be providing protection against that.
            // If the pool is filling up because of a surge of requests, the rejections
            // will just be a way of shedding load - the breaker and downstream
            // dependency may be just fine, and we want to keep them that way.

            // We'll neither mark these as success *nor* failure, since they really didn't
            // even execute as far as the breaker and downstream dependencies are
            // concerned.

            //var workItem = ThreadPool.Enqueue(() =>
            //{
            //    var token = TimeoutsIgnored
            //        ? CancellationToken.None
            //        : cancellationToken;
            //    // Since we may have been on the thread pool queue for a bit, see if we
            //    // should have canceled by now.
            //    token.ThrowIfCancellationRequested();
            //    return UseCircuitBreakers.Value
            //        ? ExecuteWithBreaker(token)
            //        : ExecuteAsync(token);
            //});

            // We could avoid passing both the token and timeout if either:
            // A. SmartThreadPool.GetResult() took a CancellationToken.
            // B. The CancellationToken provided an accessor for its Timeout.
            // C. We wrapped CancellationToken and Timeout in another class and passed it.
            // For now, this works, if a little janky.
            //using high timeout (can't use Timespan.MaxValue since this overflows) and no cancellation token when timeouts are ignored, best thing to do without changing the IWorkItem interface
            //return TimeoutsIgnored
            //    ? workItem.Get(CancellationToken.None, TimeSpan.FromMilliseconds(int.MaxValue))
            //    : workItem.Get(cancellationToken, Timeout);
        }

        private TResult ExecuteWithBulkhead<TResult>(SyncCommand<TResult> command, CancellationToken ct)
        {
            // REWRITE:
            // - Get the semaphore bulkhead for the command group
            // - Reject or increment accordingly.

            // TODO get bulkhead and check; reject if necessary
            try
            {
                // TODO increment bulkhead
                return UseCircuitBreakers.Value
                    ? ExecuteWithBreaker(command, ct)
                    : command.Execute(ct);
            }
            catch (Exception e)
            {
                // TODO decrement bulkhead
                throw;
            }
        }

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
