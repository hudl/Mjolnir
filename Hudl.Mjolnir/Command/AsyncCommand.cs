using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hudl.Mjolnir.Command
{
    /// <summary>
    /// An asynchronous command.
    /// 
    /// <seealso cref="SyncCommand{TResult}"/>
    /// 
    /// If you need both synchronous and asynchronous versions of a command, it's recommended that
    /// you implement both separately, rather than implementing one and using "conversion" code
    /// like Task.FromResult() or .Result/.Wait() (which are dangerous and can deadlock).
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by this command's execution.</typeparam>
    public abstract class AsyncCommand<TResult> : BaseCommand
    {
        public AsyncCommand(string group, string isolationKey, TimeSpan defaultTimeout) : base(group, isolationKey, defaultTimeout)
        { }

        /// <summary>
        /// The operation that should be performed when this command is invoked.
        /// 
        /// If this method throws an Exception, the command's execution will be tracked as a
        /// failure with its circuit breaker. Otherwise, it will be considered successful.
        /// </summary>
        /// <param name="cancellationToken">
        ///     Token used to cancel and detect cancellation of the Command. The token may be
        ///     provided by the Invoke call, configuration, or the default timeout given to the
        ///     command's constructor.
        /// </param>
        /// <returns>A Task providing the command's result.</returns>
        public abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);
    }
}
