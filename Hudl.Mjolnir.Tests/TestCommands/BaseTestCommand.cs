using System;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Key;
using Hudl.Mjolnir.Tests.Helper;
using Hudl.Riemann;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal abstract class BaseTestCommand<TResult> : Command<TResult>
    {
        internal BaseTestCommand() : this(TimeSpan.FromMilliseconds(10000)) { }
        internal BaseTestCommand(TimeSpan? timeout) : this(GroupKey.Named("Test"), timeout) {}
        internal BaseTestCommand(GroupKey isolationKey, TimeSpan? timeout)
            : base(isolationKey, isolationKey, timeout ?? TimeSpan.FromMilliseconds(10000))
        {
            Riemann = new IgnoringRiemannStats();
            CircuitBreaker = new AlwaysSuccessfulCircuitBreaker();
            ThreadPool = new AlwaysSuccessfulIsolationThreadPool();
            FallbackSemaphore = new AlwaysSuccessfulIsolationSemaphore();
        }
    }
}
