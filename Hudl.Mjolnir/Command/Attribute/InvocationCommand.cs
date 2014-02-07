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
            _invocation.Proceed();

            var returnType = _invocation.Method.ReturnType;

            if (typeof (Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
            {
                return (Task<TResult>)_invocation.ReturnValue;
            }

            return Task.FromResult((TResult)_invocation.ReturnValue);
        }
    }
}