using System;

namespace Hudl.Mjolnir.ThreadPool
{
    internal class IsolationThreadPoolException : Exception
    {
        public IsolationThreadPoolException(Exception cause) : base(cause.Message, cause) {}
    }

    internal class IsolationThreadPoolRejectedException : Exception {}
}