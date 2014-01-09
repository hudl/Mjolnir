using System;

namespace Hudl.Mjolnir.Tests.Helper
{
    internal class ExpectedTestException : Exception
    {
        internal ExpectedTestException(string message) : base(message) {}
        internal ExpectedTestException(string message, Exception inner) : base(message, inner) {}
    }
}
