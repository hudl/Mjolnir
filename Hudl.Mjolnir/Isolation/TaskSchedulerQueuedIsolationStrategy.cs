using System;
using System.Threading;
using System.Threading.Tasks;
using Hudl.Config;

namespace Hudl.Mjolnir.Isolation
{
    internal class TaskSchedulerQueuedIsolationStrategy : IQueuedIsolationStrategy
    {
        private readonly TaskFactory _factory;

        internal TaskSchedulerQueuedIsolationStrategy(IConfigurableValue<int> maxConcurrency, IConfigurableValue<int> maxQueueLength)
        {
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(maxConcurrency.Value, maxQueueLength.Value);
            var factory = new TaskFactory(scheduler);

            _factory = factory;
        }

        public Task<TResult> Enqueue<TResult>(Func<TResult> func, CancellationToken cancellationToken)
        {
            return _factory.StartNew(func, cancellationToken);
        }
    }
}
