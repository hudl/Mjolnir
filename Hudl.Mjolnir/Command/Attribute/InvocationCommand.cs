using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// Used by the <see cref="CommandInterceptor"/> proxy to wrap the <code>Proceed()</code>
    /// execution in a Command.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    internal class InvocationCommand<TResult> : Command<TResult>
    {
        private readonly IInvocation _invocation;

        public InvocationCommand(string group, string breakerKey, string poolKey, int timeout, IInvocation invocation)
            : base(group, breakerKey, poolKey, TimeSpan.FromMilliseconds(timeout))
        {
            _invocation = invocation;
        }

        protected override Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var returnType = _invocation.Method.ReturnType;
            var isTask = (typeof (Task).IsAssignableFrom(returnType));

            if (isTask && returnType.IsGenericType)
            {
                // TODO If the invocation supports a cancellation token, can we set it with _invocation.SetArgumentValue()?

                _invocation.Proceed();
                return (Task<TResult>)_invocation.ReturnValue;
            }

            if (isTask)
            {
                throw new NotSupportedException("Cannot invoke interceptor command for non-generic Task");
            }

            return Task.Run(() =>
            {
                _invocation.Proceed();
                return (TResult) _invocation.ReturnValue;
            }, cancellationToken);
        }
    }
}