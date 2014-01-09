using Hudl.Mjolnir.Command;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Returns the provided value from ExecuteAsync(). Should not fallback, but has a Fallback() implementation.
    /// Good for making sure that we don't call Fallback() when we shouldn't.
    /// </summary>
    internal class SuccessfulEchoCommandWithFallback : SuccessfulEchoCommandWithoutFallback
    {
        internal SuccessfulEchoCommandWithFallback(object value) : base(value) {}

        protected override object Fallback(CommandFailedException instigator)
        {
            FallbackCalled = true;
            return Value;
        }
    }
}