using System;

namespace Hudl.Mjolnir.Isolation
{
    internal class IsolationThreadPoolException : Exception
    {
        internal IsolationThreadPoolException(Exception cause) : base(cause.Message, cause) {}
    }

    internal class IsolationThreadPoolRejectedException : Exception {}
}