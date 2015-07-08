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
        internal BaseTestCommand(TimeSpan? timeout,bool ignoreTimeouts=false) : this("test", "test", timeout,ignoreTimeouts) { }
        internal BaseTestCommand(string group, string isolationKey, TimeSpan? timeout,bool ignoreTimeouts=false) : this(group, null, isolationKey, timeout,ignoreTimeouts) {}
        internal BaseTestCommand(string group, string name, string isolationKey, TimeSpan? timeout,bool ignoreTimeouts=false) : base(group, name, isolationKey, isolationKey, timeout ?? TimeSpan.FromMilliseconds(10000),ignoreTimeouts)
        {
            Stats = new IgnoringStats();
            CircuitBreaker = new AlwaysSuccessfulCircuitBreaker();
            ThreadPool = new AlwaysSuccessfulIsolationThreadPool();
            FallbackSemaphore = new AlwaysSuccessfulIsolationSemaphore();
        }
    }
}
