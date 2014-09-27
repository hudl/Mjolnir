using System;

namespace Hudl.Mjolnir.Isolation
{
    internal class IsolationThreadPoolException : Exception
    {
        internal IsolationThreadPoolException(Exception cause) : base(cause.Message, cause) {}
    }

    internal class IsolationStrategyRejectedException : Exception {}

    internal class IsolationThreadPoolRejectedException : IsolationStrategyRejectedException {}

    internal class TaskSchedulerIsolationRejectionException : IsolationStrategyRejectedException {}
}