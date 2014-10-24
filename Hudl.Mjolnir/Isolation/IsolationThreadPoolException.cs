using System;

namespace Hudl.Mjolnir.Isolation
{
    internal class IsolationThreadPoolException : Exception
    {
        internal IsolationThreadPoolException(Exception cause) : base(cause.Message, cause) {}
    }

    internal class IsolationStrategyRejectedException : Exception
    {
        internal IsolationStrategyRejectedException() { }
        internal IsolationStrategyRejectedException(string message, Exception inner) : base(message, inner) { }
    }

    // TODO This is the original class that I'm keeping around for backwards compatibility.
    // Moving forward, things should create and use IsolationStrategyRejectedExceptions instead.
    internal class IsolationThreadPoolRejectedException : IsolationStrategyRejectedException { }
}