using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Common.Extensions;
using Hudl.Config;
using Hudl.Mjolnir.Breaker;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.ThreadPool;
using Hudl.Mjolnir.Util;
using Hudl.Riemann;
using Hudl.Stats;
using log4net;
using Nito.AsyncEx;

namespace Hudl.Mjolnir.Command
{
    public abstract class Command
    {
        protected static readonly ILog Log = LogManager.GetLogger(typeof(Command<>));
        protected static readonly ConfigurableValue<bool> UseCircuitBreakers = new ConfigurableValue<bool>("mjolnir.useCircuitBreakers");
    }

    /// <summary>
    /// Protection layer for operations that might fail.
    /// 
    /// Provides isolation and fail-fast behavior around dangerous operations using timeouts,
    /// circuit breakers, and thread pools.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by this command's execution.</typeparam>
    public abstract class Command<TResult> : Command
    {
        internal readonly TimeSpan Timeout;
        private readonly GroupKey _breakerKey;
        private readonly GroupKey _poolKey;

        // Setters should be used for testing only.

        private IRiemann _riemann;
        internal IRiemann Riemann
        {
            private get { return _riemann ?? RiemannStats.Instance; }
            set { _riemann = value; }
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
        private int _hasInvoked = 0;

        /// <summary>
        /// Constructs the command.
        /// 
        /// The provided <code>isolationKey</code> will be used as both the
        /// breaker and pool keys.
        /// 
        /// The key will be something like "Mongo", "Recruit", "Football", "Stripe", "Mongo-SpecificCollection", etc.
        /// </summary>
        /// <param name="isolationKey">Breaker and pool key to use.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
        protected Command(GroupKey isolationKey, TimeSpan defaultTimeout) : this(isolationKey, isolationKey, defaultTimeout) {}

        /// <summary>
        /// Constructs the command.
        /// 
        /// The key will be something like "Mongo", "Recruit", "Football", "Stripe", "Mongo-SpecificCollection", etc.
        /// </summary>
        /// <param name="breakerKey">Breaker to use for this command.</param>
        /// <param name="poolKey">Pool to use for this command.</param>
        /// <param name="defaultTimeout">Timeout to enforce if not otherwise configured.</param>
        protected Command(GroupKey breakerKey, GroupKey poolKey, TimeSpan defaultTimeout)
        {
            if (breakerKey == null)
            {
                throw new ArgumentNullException("breakerKey");
            }

            if (poolKey == null)
            {
                throw new ArgumentNullException("poolKey");
            }

            if (defaultTimeout.TotalMilliseconds <= 0)
            {
                throw new ArgumentException("Positive default timeout is required", "defaultTimeout");
            }

            var timeout = new ConfigurableValue<long>("command." + Name + ".Timeout").Value;
            if (timeout <= 0)
            {
                timeout = (long)defaultTimeout.TotalMilliseconds;
            }

            Timeout = TimeSpan.FromMilliseconds(timeout);

            _breakerKey = breakerKey;
            _poolKey = poolKey;
        }

        private string _name;
        internal string Name
        {
            get
            {
                if (_name != null)
                {
                    return _name;
                }

                var lastAssemblyPart = NamingUtil.GetLastAssemblyPart(GetType());
                var name = GetType().Name;
                if (name.EndsWith("Command", StringComparison.InvariantCulture))
                {
                    name = name.Substring(0, name.LastIndexOf("Command", StringComparison.InvariantCulture));
                }
                
                _name = lastAssemblyPart + "." + name;
                return _name;
            }
        }

        internal GroupKey BreakerKey
        {
            get { return _breakerKey; }
        }

        internal GroupKey PoolKey
        {
            get { return _poolKey; }
        }

        private string RiemannKeyPrefix
        {
            get { return "mjolnir command " + Name; }
        }

        /// <summary>
        /// Synchronous pass-through to InvokeAsync().
        /// </summary>
        /// <returns></returns>
        public TResult Invoke()
        {
            return AsyncContext.Run(() => InvokeAsync());
        }

        /// <summary>
        /// Runs this command, returning the result or throwing an exception if the command failed
        /// or couldn't be completed.
        /// 
        /// <b>Important:</b> You <b>MUST</b> await on this method; <b>do not block on the returned <code>Task</code></b>.<br/>
        /// Calling a blocking method on the task (e.g. <code>Task.Result</code> or <code>Wait()</code>) will cause
        /// deadlocks in your code.
        /// </summary>
        public async Task<TResult> InvokeAsync()
        {
            if (Interlocked.CompareExchange(ref _hasInvoked, 1, 0) > 0)
            {
                throw new InvalidOperationException("A command instance may only be invoked once");
            }

            var invokeStopwatch = Stopwatch.StartNew();
            var executeStopwatch = Stopwatch.StartNew();
            var status = CommandCompletionStatus.RanToCompletion;
            try
            {
                Log.InfoFormat("InvokeAsync Command={0} Breaker={1} Pool={2} Timeout={3}", Name, BreakerKey, PoolKey, Timeout.TotalMilliseconds);

                var cancellationTokenSource = new CancellationTokenSource(Timeout);
                var result = await ExecuteInIsolation(cancellationTokenSource.Token).ConfigureAwait(false);
                executeStopwatch.Stop();
                return result;
            }
            catch (Exception e)
            {
                executeStopwatch.Stop();
                status = StatusFromException(e);

                var instigator = new CommandFailedException(e, status).WithData(new
                {
                    Command = Name,
                    Timeout = Timeout.TotalMilliseconds,
                    Status = status,
                    Breaker = BreakerKey,
                    Pool = PoolKey,
                });

                // Log here because we don't know if a fallback implementer is going to rethrow this or eat it.
                Log.Error(instigator);
                return TryFallback(instigator);
            }
            finally
            {
                invokeStopwatch.Stop();

                Riemann.Elapsed(RiemannKeyPrefix + " ExecuteInIsolation", status.ToString(), executeStopwatch.Elapsed);
                Riemann.Elapsed(RiemannKeyPrefix + " InvokeAsync", status.ToString(), invokeStopwatch.Elapsed);

                StatsDClient.ManualTime("mjolnir.command." + Name + ".execute." + status, invokeStopwatch.ElapsedMilliseconds);
                StatsDClient.ManualTime("mjolnir.command." + Name + ".invoke." + status, invokeStopwatch.ElapsedMilliseconds);
            }
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
                // Since we may have been on the thread pool queue for a bit, see if we
                // should have canceled by now.
                cancellationToken.ThrowIfCancellationRequested();

                return UseCircuitBreakers.Value
                    ? ExecuteWithBreaker(cancellationToken)
                    : ExecuteAsync(cancellationToken);
            });

            // We could avoid passing both the token and timeout if either:
            // A. SmartThreadPool.GetResult() took a CancellationToken.
            // B. The CancellationToken provided an accessor for its Timeout.
            // C. We wrapped CancellationToken and Timeout in another class and passed it.
            // For now, this works, if a little janky.
            return workItem.Get(cancellationToken, Timeout);
        }

        private Task<TResult> ExecuteWithBreaker(CancellationToken cancellationToken)
        {
            if (!CircuitBreaker.IsAllowing())
            {
                throw new CircuitBreakerRejectedException();
            }

            Task<TResult> result;

            try
            {
                var stopwatch = Stopwatch.StartNew();
                result = ExecuteAsync(cancellationToken);
                CircuitBreaker.MarkSuccess(stopwatch.ElapsedMilliseconds);
                CircuitBreaker.Metrics.MarkCommandSuccess();
            }
            catch (Exception)
            {
                CircuitBreaker.Metrics.MarkCommandFailure();
                throw;
            }

            return result;
        }

        private static CommandCompletionStatus StatusFromException(Exception e)
        {
            if (IsCancellationException(e))
            {
                return CommandCompletionStatus.Canceled;
            }

            if (e is CircuitBreakerRejectedException || e is IsolationThreadPoolRejectedException)
            {
                return CommandCompletionStatus.Rejected;
            }

            return CommandCompletionStatus.Faulted;
        }

        private static bool IsCancellationException(Exception e)
        {
            return (e is TaskCanceledException || e is OperationCanceledException);
        }

        private TResult TryFallback(CommandFailedException instigator)
        {
            var stopwatch = Stopwatch.StartNew();
            var fallbackStatus = FallbackStatus.Success;

            var semaphore = FallbackSemaphore; // Locally reference in case the property gets updated (highly unlikely).
            if (!semaphore.TryEnter())
            {
                Riemann.Elapsed(RiemannKeyPrefix + " TryFallback", FallbackStatus.Rejected.ToString(), stopwatch.Elapsed);
                StatsDClient.ManualTime("mjolnir.command." + Name + ".fallback." + FallbackStatus.Rejected, stopwatch.ElapsedMilliseconds);

                instigator.FallbackStatus = FallbackStatus.Rejected;
                throw instigator;
            }
            
            try
            {
                return Fallback(instigator);
            }
            catch (Exception e)
            {
                var cfe = e as CommandFailedException;

                if (cfe != null && !cfe.IsFallbackImplemented)
                {
                    // This was rethrown from the default Fallback() implementation (here in the Command class).
                    fallbackStatus = FallbackStatus.NotImplemented;
                }
                else
                {
                    fallbackStatus = FallbackStatus.Failure;
                }

                if (cfe != null)
                {
                    cfe.FallbackStatus = fallbackStatus;
                }

                throw;
            }
            finally
            {
                semaphore.Release();

                stopwatch.Stop();
                Riemann.Elapsed(RiemannKeyPrefix + " TryFallback", fallbackStatus.ToString(), stopwatch.Elapsed);
                StatsDClient.ManualTime("mjolnir.command." + Name + ".fallback." + fallbackStatus, stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// The operation that should be performed when this command is invoked.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel and detect cancellation of the command.</param>
        /// <returns>A Task that will provide the command's result.</returns>
        protected abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// May be optionally implemented. Will be invoked if <see cref="ExecuteAsync(CancellationToken)"/> fails
        /// (for any reason: timeout, fault, rejected, etc.).
        /// 
        /// If you need to make another service (or other potentially-latent) call in the fallback, make sure
        /// to do it via a Command.
        /// 
        /// Although the triggering Exception (<see cref="instigator"/>) is provided, you don't have to use it. You
        /// may ignore it, rethrow it, wrap it, etc.
        /// 
        /// Any exception thrown from this method will propagate up to the <code>Command</code> caller.
        /// </summary>
        /// <param name="instigator">The exception that triggered the fallback.</param>
        /// <returns>Result, likely from an alternative source (cache, solr, etc.).</returns>
        protected virtual TResult Fallback(CommandFailedException instigator)
        {
            instigator.IsFallbackImplemented = false;
            throw instigator;
        }
    }
}
