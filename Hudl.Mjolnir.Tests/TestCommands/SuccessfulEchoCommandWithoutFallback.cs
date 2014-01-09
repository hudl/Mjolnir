using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Returns the provided value from ExecuteAsync().
    /// </summary>
    internal class SuccessfulEchoCommandWithoutFallback : BaseTestCommand<object>
    {
        protected readonly object Value;
        internal bool FallbackCalled = false;

        internal SuccessfulEchoCommandWithoutFallback(object value)
        {
            Value = value;
        }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => Value, cancellationToken);
        }
    }
}