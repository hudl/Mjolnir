using System.Threading;
using System.Threading.Tasks;
using Hudl.Mjolnir.Tests.Helper;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    /// <summary>
    /// Throws the exception it's given.
    /// </summary>
    internal class EchoThrowingCommandWithoutFallback : BaseTestCommand<object>
    {
        private readonly ExpectedTestException _exception;

        internal EchoThrowingCommandWithoutFallback(ExpectedTestException toRethrow)
        {
            _exception = toRethrow;
        }

        protected override Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}