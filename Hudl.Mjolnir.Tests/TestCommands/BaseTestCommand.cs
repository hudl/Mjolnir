using System;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.External;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal abstract class BaseTestCommand<TResult> : Command<TResult>
    {
        internal BaseTestCommand() : this(TimeSpan.FromMilliseconds(10000)) { }
        internal BaseTestCommand(TimeSpan? timeout) : this("test", "test", timeout) {}
        internal BaseTestCommand(string group, string isolationKey, TimeSpan? timeout) : this(group, null, isolationKey, timeout) {}
        internal BaseTestCommand(string group, string name, string isolationKey, TimeSpan? timeout) : base(group, name, isolationKey, isolationKey, timeout ?? TimeSpan.FromMilliseconds(10000))
        {
            Stats = new IgnoringStats();
            CircuitBreaker = new AlwaysSuccessfulCircuitBreaker();
            IsolationStrategy = new AlwaysSuccessfulIsolationThreadPool();
            FallbackSemaphore = new AlwaysSuccessfulIsolationSemaphore();
        }
    }
}
