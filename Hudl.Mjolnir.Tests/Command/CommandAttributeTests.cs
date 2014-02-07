using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Tests.Helper;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command
{
    public class CommandAttributeTests : TestFixture
    {
        [Fact]
        public void String_WithImplementation()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);
            
            var result = proxy.InvokeString();

            Assert.True(instance.IsCompleted);
            Assert.Equal("Non-task string", result);
        }

        [Fact]
        public async Task GenericTask_WithImplementation()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            var task = proxy.InvokeGenericTask();
            var result = await task;

            Assert.True(instance.IsCompleted);
            Assert.Equal("Generic task string", result);
        }

        [Fact]
        public async Task GenericTask_WithImplementation_RunAndSleep()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            var task = proxy.InvokeGenericTaskWithRunAndSleep();
            await task;

            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public async Task GenericTask_WithImplementation_AwaitDelay()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            var task = proxy.InvokeGenericTaskWithAwaitDelay();
            await task;

            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public async Task UntypedTask_WithImplementation()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            var task = proxy.InvokeUntypedTask();

            Assert.True(task != null);
            Assert.True(task is Task);
            Assert.False(task.GetType().IsGenericType);

            await task;

            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public void Void_WithImplementation()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            proxy.InvokeVoid();

            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public void NoImplementation_Throws()
        {
            var proxy = CreateForNoImplementation();

            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                proxy.InvokeGenericTask();
            });

            Assert.Equal("Invocation target required", ex.Message);
        }

        [Fact]
        public void InterfaceMissingAttribute_Throws()
        {
            var instance = new TestImplementation();
            var proxy = CreateForNoAttribute(instance);

            var ex = Assert.Throws<TargetInvocationException>(() =>
            {
                proxy.InvokeGenericTask();
            });

            Assert.Equal("Interface does not have [CommandAttribute]", ex.InnerException.Message);
        }

        [Fact]
        public void TimeoutPresentOnInterfaceMethod_OverridesDefaultTimeout()
        {
            // TODO
        }

        [Fact]
        public void TimeoutPresentOnImplementationMethod_IsIgnored()
        {
            // TODO
        }

        private ITestInterfaceWithImplementation CreateForImplementation(TestImplementation instance)
        {
            return CommandInterceptor.CreateProxy<ITestInterfaceWithImplementation>(instance);
        }

        private ITestInterfaceWithoutImplementation CreateForNoImplementation()
        {
            var generator = new ProxyGenerator();
            return (ITestInterfaceWithoutImplementation) generator.CreateInterfaceProxyWithoutTarget(typeof(ITestInterfaceWithoutImplementation), new CommandInterceptor());
        }

        private ITestInterfaceWithoutAttribute CreateForNoAttribute(TestImplementation instance)
        {
            var generator = new ProxyGenerator();
            return (ITestInterfaceWithoutAttribute)generator.CreateInterfaceProxyWithTarget(typeof(ITestInterfaceWithoutAttribute), instance, new CommandInterceptor());
        }
    }

    [Command("foo", "bar", "baz", 10000)]
    public interface ITestInterfaceWithImplementation
    {
        Task<string> InvokeGenericTask();
        Task<object> InvokeGenericTaskWithRunAndSleep();
        Task<object> InvokeGenericTaskWithAwaitDelay();
        Task InvokeUntypedTask();
        string InvokeString();
        void InvokeVoid();
    }

    public class TestImplementation : ITestInterfaceWithImplementation, ITestInterfaceWithoutAttribute
    {
        public bool IsCompleted { get; private set; }

        public TestImplementation()
        {
            IsCompleted = false;
        }

        public Task<string> InvokeGenericTask()
        {
            IsCompleted = true;
            return Task.FromResult("Generic task string");
        }

        public Task<object> InvokeGenericTaskWithRunAndSleep()
        {
            return Task.Run(() =>
            {
                Thread.Sleep(100);
                IsCompleted = true;
                return new object();
            });
        }

        public async Task<object> InvokeGenericTaskWithAwaitDelay()
        {
            await Task.Delay(100);
            IsCompleted = true;
            return new object();
        }

        public Task InvokeUntypedTask()
        {
            return Task.Run(() =>
            {
                IsCompleted = true;
            });
        }

        public string InvokeString()
        {
            IsCompleted = true;
            return "Non-task string";
        }

        public void InvokeVoid()
        {
            // Nothing.
            IsCompleted = true;
        }
    }

    [Command("foo", "bar", "baz", 10000)]
    public interface ITestInterfaceWithoutImplementation
    {
        Task<string> InvokeGenericTask();
    }

    public interface ITestInterfaceWithoutAttribute
    {
        Task<string> InvokeGenericTask();
    }
}
