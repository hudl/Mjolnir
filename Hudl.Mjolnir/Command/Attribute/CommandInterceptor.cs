using System;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// <see cref="CreateProxy"/>
    /// </summary>
    public class CommandInterceptor : IInterceptor
    {
        private readonly MethodInfo _invokeCommandAsyncMethod = typeof(CommandInterceptor).GetMethod("InvokeCommandAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo _invokeCommandSyncMethod = typeof(CommandInterceptor).GetMethod("InvokeCommandSync", BindingFlags.NonPublic | BindingFlags.Instance);

        public void Intercept(IInvocation invocation)
        {
            if (invocation.InvocationTarget == null)
            {
                throw new InvalidOperationException("Invocation target required");
            }

            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof (void))
            {
                var isFireAndForget = (invocation.Method.GetCustomAttribute<FireAndForgetAttribute>(false) != null);
                if (isFireAndForget)
                {
                    // Run async and don't await the result.
                    _invokeCommandAsyncMethod.MakeGenericMethod(typeof (VoidResult)).Invoke(this, new object[] { invocation });
                }
                else
                {
                    _invokeCommandSyncMethod.MakeGenericMethod(typeof (VoidResult)).Invoke(this, new object[] { invocation });
                }
                return;
            }
            
            if (typeof(Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
            {
                var innerType = returnType.GetGenericArguments()[0];
                invocation.ReturnValue = _invokeCommandAsyncMethod.MakeGenericMethod(innerType).Invoke(this, new object[] { invocation });
                return;
            }

            if (typeof(Task).IsAssignableFrom(returnType)) // Non-generic task.
            {
                // This case gets weird, and it's rare that we'd need to support it.
                // Leaving it alone for now.

                throw new NotSupportedException("Non-generic Tasks are not supported, consider using void with [FireAndForget]");

                //var method = _invokeCommandAsyncMethod.MakeGenericMethod(typeof (VoidResult));

                //// This is kind of jank.
                //var task = Task.Factory.StartNew((Action)(() => method.Invoke(this, new object[] { invocation })));
                //invocation.ReturnValue = task;
                //return;
            }

            invocation.ReturnValue = _invokeCommandSyncMethod.MakeGenericMethod(returnType).Invoke(this, new object[] { invocation });
        }

        // ReSharper disable UnusedMember.Local - Used via reflection.
        private Task<T> InvokeCommandAsync<T>(IInvocation invocation)
        {
            // ReSharper restore UnusedMember.Local
            var command = CreateCommand<T>(invocation);
            return command.InvokeAsync();
        }

        // ReSharper disable UnusedMember.Local - Used via reflection.
        private T InvokeCommandSync<T>(IInvocation invocation)
        {
            // ReSharper restore UnusedMember.Local
            var command = CreateCommand<T>(invocation);
            return command.Invoke();
        }

        private Command<T> CreateCommand<T>(IInvocation invocation)
        {
            var attribute = invocation.Method.DeclaringType.GetCustomAttribute<CommandAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException("Interface does not have [CommandAttribute]");
            }

            var timeoutAttribute = invocation.Method.GetCustomAttribute<CommandTimeoutAttribute>();

            return new InvocationCommand<T>(
                attribute.Group,
                attribute.BreakerKey,
                attribute.PoolKey,
                timeoutAttribute != null ? timeoutAttribute.Timeout : attribute.Timeout,
                invocation);
        }

        /// <summary>
        /// Creates a CommandInterceptor proxy for an interface of type <code>T</code>
        /// and its corresponding implementation instance.
        /// </summary>
        /// <typeparam name="T">Interface type to create proxy for</typeparam>
        /// <param name="instance">Target implementation instance to use within the proxy</param>
        /// <returns>CommandInterceptor proxy of type <code>T</code></returns>
        public static T CreateProxy<T>(T instance) where T : class
        {
            return (T)CreateProxy(typeof (T), instance);
        }

        /// <summary>
        /// Creates a CommandInterceptor proxy for an interface of the provided type
        /// and its corresponding implementation instance.
        /// </summary>
        /// <param name="interfaceType">Interface type to create proxy for</param>
        /// <param name="instance">Target implementation instance to use within the proxy. Should implement the provided interface type.</param>
        /// <returns>CommandInterceptor proxy of the interface type provided.</returns>
        public static object CreateProxy(Type interfaceType, object instance)
        {
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException("Proxies may only be created for interfaces");
            }

            var generator = new ProxyGenerator();
            return generator.CreateInterfaceProxyWithTarget(interfaceType, instance, new CommandInterceptor());
        }
    }
}
