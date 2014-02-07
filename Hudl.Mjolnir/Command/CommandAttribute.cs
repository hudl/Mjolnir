using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Hudl.Mjolnir.Command
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
    public sealed class CommandAttribute : Attribute
    {
        private const int DefaultTimeout = 15000;

        private readonly string _group;
        private readonly string _breakerKey;
        private readonly string _poolKey;
        private readonly int _timeout;

        public CommandAttribute(string group, string isolationKey, int timeout = DefaultTimeout) : this(group, isolationKey, isolationKey, timeout) {}

        public CommandAttribute(string group, string breakerKey, string poolKey, int timeout = DefaultTimeout)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                throw new ArgumentException("group");
            }

            if (string.IsNullOrWhiteSpace(breakerKey))
            {
                throw new ArgumentException("breakerKey");
            }

            if (string.IsNullOrWhiteSpace(poolKey))
            {
                throw new ArgumentNullException("poolKey");
            }

            if (timeout < 0)
            {
                throw new ArgumentException("timeout");
            }

            _group = group;
            _breakerKey = breakerKey;
            _poolKey = poolKey;
            _timeout = timeout;
        }

        public string Group
        {
            get { return _group; }
        }

        public string BreakerKey
        {
            get { return _breakerKey; }
        }

        public string PoolKey
        {
            get { return _poolKey; }
        }

        public int Timeout
        {
            get { return _timeout; }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CommandTimeout : Attribute
    {
        private readonly int _timeout;

        public CommandTimeout(int timeout)
        {
            if (timeout < 0)
            {
                throw new ArgumentException("timeout");
            }

            _timeout = timeout;
        }

        public int Timeout
        {
            get { return _timeout; }
        }
    }

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
                // TODO Consider whether this should be a sync or async call. Do callers expect the call to block until the operation is done? Likely.
                // Maybe we could introduce a [FireAndForget] attribute that would force async and return immediately.
                _invokeCommandSyncMethod.MakeGenericMethod(typeof (VoidResult)).Invoke(this, new object[] { invocation });
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
                var method = _invokeCommandAsyncMethod.MakeGenericMethod(typeof (VoidResult));

                // This is kind of jank.
                var task = Task.Factory.StartNew((Action)(() => method.Invoke(this, new object[] { invocation })));
                invocation.ReturnValue = task;
                return;
            }

            invocation.ReturnValue = _invokeCommandSyncMethod.MakeGenericMethod(returnType).Invoke(this, new object[] { invocation });
        }

        private Task<T> InvokeCommandAsync<T>(IInvocation invocation)
        {
            var command = CreateCommand<T>(invocation);
            return command.InvokeAsync();
        }

        private T InvokeCommandSync<T>(IInvocation invocation)
        {
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

            var timeoutAttribute = invocation.Method.GetCustomAttribute<CommandTimeout>();

            return new InvocationCommand<T>(
                attribute.Group,
                attribute.BreakerKey,
                attribute.PoolKey,
                timeoutAttribute != null ? timeoutAttribute.Timeout : attribute.Timeout,
                invocation);
        }

        public static T CreateProxy<T>(T instance) where T : class
        {
            return (T)CreateProxy(typeof (T), instance);
        }

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
