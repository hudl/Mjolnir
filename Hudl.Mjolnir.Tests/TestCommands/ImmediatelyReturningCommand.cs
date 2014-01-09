using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Tests.TestCommands
{
    internal class ImmediatelyReturningCommandWithoutFallback : BaseTestCommand<object>
    {
        protected override async Task<object> ExecuteAsync(CancellationToken cancellationToken)
        {
            return await Task.Run<object>(() => new { }, cancellationToken);
        }
    }
}
