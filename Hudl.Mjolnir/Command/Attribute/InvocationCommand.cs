using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public InvocationCommand(string group, string name, string breakerKey, string poolKey, int timeout, IInvocation invocation)
            : base(group, name, breakerKey, poolKey, TimeSpan.FromMilliseconds(timeout))
        {
            _invocation = invocation;
        }

        protected override Task<TResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var returnType = _invocation.Method.ReturnType;
            var isTaskReturnType = (typeof (Task).IsAssignableFrom(returnType));

            // Cancellation support is kind of wonky. It works, but it relies on
            // there being a CancellationToken parameter in the signature of the
            // method being proxied. Not terrible, but it's a bit noisy, and
            // isn't entirely obvious that you can do it.

            // We could potentially solve this by using a common token that
            // flows with the ExecutionContext or something.

            // Do we have a CancellationToken property? If so, where is it? Will be -1 if not found.
            var cancellationTokenIndex = new List<ParameterInfo>(_invocation.Method.GetParameters()).FindLastIndex(IsCancellationToken);

            // If we have one that doesn't already have a value, lets give it ours.
            if (cancellationTokenIndex >= 0 && IsReplaceableToken(_invocation.Arguments[cancellationTokenIndex]))
            {
                _invocation.SetArgumentValue(cancellationTokenIndex, cancellationToken);
            }

            if (isTaskReturnType && returnType.IsGenericType)
            {
                _invocation.Proceed();
                return (Task<TResult>)_invocation.ReturnValue;
            }

            if (isTaskReturnType)
            {
                throw new NotSupportedException("Cannot invoke interceptor command for non-generic Task");
            }

            return Task.Run(() =>
            {
                _invocation.Proceed();
                return (TResult) _invocation.ReturnValue;
            }, cancellationToken);
        }

        private static bool IsCancellationToken(ParameterInfo parameter)
        {
            return typeof (CancellationToken).IsAssignableFrom(parameter.ParameterType) ||
                   typeof (CancellationToken?).IsAssignableFrom(parameter.ParameterType);
        }

        private static bool IsReplaceableToken(object cancellationToken)
        {
            return cancellationToken == null || (CancellationToken?)cancellationToken == CancellationToken.None;
        }
    }
}