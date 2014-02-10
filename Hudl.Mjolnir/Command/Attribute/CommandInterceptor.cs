using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using log4net;

namespace Hudl.Mjolnir.Command.Attribute
{
    /// <summary>
    /// <see cref="CreateProxy"/>
    /// </summary>
    public class CommandInterceptor : IInterceptor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (CommandInterceptor));
        private static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();
        private readonly MethodInfo _invokeCommandAsyncMethod = typeof(CommandInterceptor).GetMethod("InvokeCommandAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo _invokeCommandSyncMethod = typeof(CommandInterceptor).GetMethod("InvokeCommandSync", BindingFlags.NonPublic | BindingFlags.Instance);

        public void Intercept(IInvocation invocation)
        {
            if (invocation.InvocationTarget == null)
            {
                throw new InvalidOperationException("Invocation target required");
            }

            try
            {
                var returnType = invocation.Method.ReturnType;
                if (returnType == typeof (void))
                {
                    var isFireAndForget = (invocation.Method.GetCustomAttribute<FireAndForgetAttribute>(false) != null);
                    if (isFireAndForget)
                    {
                        try
                        {
                            // Run async and don't await the result.
                            _invokeCommandAsyncMethod.MakeGenericMethod(typeof(VoidResult)).Invoke(this, new object[] { invocation });    
                        }
                        catch (Exception e)
                        {
                            // Even though we're going to async this off in the command, the default TaskScheduler
                            // doesn't guarantee that it's going to run on a separate thread; it *may* execute inline on
                            // this thread. That means that if the execution throws an exception, it'll propagate back
                            // up to the caller, which the caller's probably not expecting.

                            // Instead of rethrowing, we'll just handle and log it here. A future improvement would be
                            // to write a custom scheduler that would always launch the task on a background thread.
                            Log.Error("Caught exception invoking FireAndForget method", e);
                        }
                    }
                    else
                    {
                        _invokeCommandSyncMethod.MakeGenericMethod(typeof (VoidResult)).Invoke(this, new object[] { invocation });
                    }
                    return;
                }

                if (typeof (Task).IsAssignableFrom(returnType) && returnType.IsGenericType)
                {
                    var innerType = returnType.GetGenericArguments()[0];
                    invocation.ReturnValue = _invokeCommandAsyncMethod.MakeGenericMethod(innerType).Invoke(this, new object[] { invocation });
                    return;
                }

                if (typeof (Task).IsAssignableFrom(returnType)) // Non-generic task.
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
            catch (TargetInvocationException e)
            {
                if (e.InnerException is CommandFailedException)
                {
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }

                throw;
            }
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
        /// 
        /// Passing a ProxyGenerator is recommended since the generator internally caches
        /// things to speed up runtime proxy creation.
        /// </summary>
        /// <typeparam name="T">Interface type to create proxy for</typeparam>
        /// <param name="instance">Target implementation instance to use within the proxy</param>
        /// /// <param name="proxyGenerator">ProxyGenerator to use. If not provided, a new one will be created.</param>
        /// <returns>CommandInterceptor proxy of type <code>T</code></returns>
        public static T CreateProxy<T>(T instance, ProxyGenerator proxyGenerator = null) where T : class
        {
            return (T)CreateProxy(typeof (T), instance, proxyGenerator);
        }

        /// <summary>
        /// Creates a CommandInterceptor proxy for an interface of the provided type
        /// and its corresponding implementation instance.
        /// 
        /// Passing a ProxyGenerator is recommended since the generator internally caches
        /// things to speed up runtime proxy creation.
        /// </summary>
        /// <param name="interfaceType">Interface type to create proxy for</param>
        /// <param name="instance">Target implementation instance to use within the proxy. Should implement the provided interface type.</param>
        /// <param name="proxyGenerator">ProxyGenerator to use. If not provided, a new one will be created.</param>
        /// <returns>CommandInterceptor proxy of the interface type provided.</returns>
        public static object CreateProxy(Type interfaceType, object instance, ProxyGenerator proxyGenerator = null)
        {
            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException("Proxies may only be created for interfaces");
            }

            var generator = proxyGenerator ?? ProxyGenerator;
            return generator.CreateInterfaceProxyWithTarget(interfaceType, instance, new CommandInterceptor());
        }
    }
}
