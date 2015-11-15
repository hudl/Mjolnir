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
        internal readonly TimeSpan Timeout;

        //private readonly ILog _log;

        private readonly GroupKey _group;
        private readonly string _name;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _poolKey;
        protected readonly bool TimeoutsIgnored;

        // Setters should be used for testing only.

        private IStats _stats;
        internal IStats Stats
        {
            private get { return _stats ?? CommandContext.Stats; }
            set { _stats = value; }
        }

        private ICircuitBreaker _breaker;
        internal ICircuitBreaker CircuitBreaker
        {
            private get { return _breaker ?? CommandContext.GetCircuitBreaker(_breakerKey); }
            set { _breaker = value; }
        }

        private IIsolationThreadPool _pool;
        internal IIsolationThreadPool ThreadPool
        {
            private get { return _pool ?? CommandContext.GetThreadPool(_poolKey); }
            set { _pool = value; }
        }

        private IIsolationSemaphore _fallbackSemaphore;
        internal IIsolationSemaphore FallbackSemaphore
        {
            // TODO Consider isolating these per-command instead of per-pool.
            private get { return _fallbackSemaphore ?? CommandContext.GetFallbackSemaphore(_poolKey); }
            set { _fallbackSemaphore = value; }
        }

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

            //_log = LogManager.GetLogger("Hudl.Mjolnir.Command." + _name);

            TimeoutsIgnored = IgnoreCommandTimeouts.Value;
            if (TimeoutsIgnored)
            {
                //_log.Debug("Creating command with timeout disabled.");
                return;
            }
            var timeout = GetTimeoutConfigurableValue(_name).Value;
            if (timeout <= 0)
            {
                timeout = (long)defaultTimeout.TotalMilliseconds;
            }
            else
            {
                //_log.DebugFormat("Timeout configuration override for this command of {0}", timeout);
            }
            Timeout = TimeSpan.FromMilliseconds(timeout);
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

        private string StatsPrefix
        {
            get { return "mjolnir command " + Name; }
        }
    }
    
    public abstract class AsyncCommand<TResult> : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }

    public abstract class AsyncCommand : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }

    public abstract class SyncCommand<TResult> : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected abstract TResult Execute(CancellationToken cancellationToken);
    }

    public abstract class SyncCommand : BaseCommand
    {
        public SyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        protected abstract void Execute(CancellationToken cancellationToken);
    }

    public interface ICommandInvoker
    {
        Task Invoke(AsyncCommand command);
        Task<TResult> Invoke<TResult>(AsyncCommand<TResult> command);
        void Invoke(SyncCommand command);
        TResult Invoke<TResult>(SyncCommand<TResult> command);
    }
    
    public class CommandInvoker : ICommandInvoker
    {
        public Task Invoke(AsyncCommand command)
        {
            // TODO
        }

        public Task<TResult> Invoke<TResult>(AsyncCommand<TResult> command)
        {
            if (Interlocked.CompareExchange(ref command._hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }

            var log = LogManager.GetLogger("Hudl.Mjolnir.Command." + command.Name);

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;
            var cancellationTokenSource = new CancellationTokenSource(command.Timeout);
            try
            {
                log.InfoFormat("InvokeAsync Command={0} Breaker={1} Pool={2} Timeout={3}", command.Name, command.BreakerKey, command.PoolKey, command.Timeout.TotalMilliseconds);

                // Note: this actually awaits the *enqueueing* of the task, not the task execution itself.
                var result = await ExecuteInIsolation(cancellationTokenSource.Token).ConfigureAwait(false);
                executeStopwatch.Stop();
                return result;
            }
            catch (Exception e)
            {
                var tokenSourceCancelled = cancellationTokenSource.IsCancellationRequested;
                executeStopwatch.Stop();
                var instigator = GetCommandFailedException(e, tokenSourceCancelled, out status).WithData(new
                {
                    Command = Name,
                    Timeout = Timeout.TotalMilliseconds,
                    Status = status,
                    Breaker = BreakerKey,
                    Pool = PoolKey,
                });

                // We don't log the exception here - that's intentional.

                // If a fallback is not implemented, the exception will get re-thrown and (hopefully) caught
                // and logged by an upstream container. This is the majority of cases, so logging here
                // results in a lot of extra, unnecessary logs and stack traces.

                // If a fallback is implemented, the burden is on the implementation to log or rethrow the
                // exception. Otherwise it'll be eaten. This is documented on the Fallback() method.

                return TryFallback(instigator);
            }
            finally
            {
                invokeStopwatch.Stop();

                Stats.Elapsed(StatsPrefix + " execute", status.ToString(), executeStopwatch.Elapsed);
                Stats.Elapsed(StatsPrefix + " total", status.ToString(), invokeStopwatch.Elapsed);
            }
        }

        public void Invoke(SyncCommand command)
        {
            // TODO
        }

        public TResult Invoke<TResult>(SyncCommand<TResult> command)
        {
            // TODO
        }

        private Task<TResult> ExecuteInIsolation(CancellationToken cancellationToken)
        {
            // Note: Thread pool rejections shouldn't count as failures to the breaker.
            // If a downstream dependency is slow, the pool will fill up, but the
            // breaker + timeouts will already be providing protection against that.
            // If the pool is filling up because of a surge of requests, the rejections
            // will just be a way of shedding load - the breaker and downstream
            // dependency may be just fine, and we want to keep them that way.

            // We'll neither mark these as success *nor* failure, since they really didn't
            // even execute as far as the breaker and downstream dependencies are
            // concerned.

            var workItem = ThreadPool.Enqueue(() =>
            {
                var token = TimeoutsIgnored
                    ? CancellationToken.None
                    : cancellationToken;
                // Since we may have been on the thread pool queue for a bit, see if we
                // should have canceled by now.
                token.ThrowIfCancellationRequested();
                return UseCircuitBreakers.Value
                    ? ExecuteWithBreaker(token)
                    : ExecuteAsync(token);
            });

            // We could avoid passing both the token and timeout if either:
            // A. SmartThreadPool.GetResult() took a CancellationToken.
            // B. The CancellationToken provided an accessor for its Timeout.
            // C. We wrapped CancellationToken and Timeout in another class and passed it.
            // For now, this works, if a little janky.
            //using high timeout (can't use Timespan.MaxValue since this overflows) and no cancellation token when timeouts are ignored, best thing to do without changing the IWorkItem interface
            return TimeoutsIgnored
                ? workItem.Get(CancellationToken.None, TimeSpan.FromMilliseconds(int.MaxValue))
                : workItem.Get(cancellationToken, Timeout);
        }
    }
}
