using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Hudl.Mjolnir.Command;
using Hudl.Mjolnir.Command.Attribute;
using Hudl.Mjolnir.Tests.Helper;
using Moq;
using Xunit;

namespace Hudl.Mjolnir.Tests.Command.Attribute
{
    public class CommandAttributeAndProxyTests : TestFixture
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
        public void String_WithThrowingImplementation_ThrowsCommandFailedException()
        {
            var exception = new ExpectedTestException("Expected");
            var instance = new TestThrowingImplementation(exception);
            var proxy = CreateForImplementation(instance);

            var ex = Assert.Throws<CommandFailedException>(() =>
            {
                proxy.InvokeString();
            });

            Assert.Equal(exception, ex.InnerException);
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
        public async Task GenericTask_WithThrowingImplementation_ThrowsCommandFailedExceptionOnAwait()
        {
            var exception = new ExpectedTestException("Expected");
            var instance = new TestThrowingImplementation(exception);
            var proxy = CreateForImplementation(instance);

            var task = proxy.InvokeGenericTask();

            try
            {
                // Using Assert.Throws(async () => {}) doesn't work right here,
                // so falling back to the ol' try/catch.
                await task;
            }
            catch (CommandFailedException e)
            {
                Assert.Equal(exception, e.InnerException);
                return;
            }

            AssertX.FailExpectedException();
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
        public async Task UntypedTask_WithImplementation_Throws()
        {
            var instance = new TestImplementation();
            var proxy = CreateForImplementation(instance);

            Assert.Throws<NotSupportedException>(() => { proxy.InvokeUntypedTask(); });

            //Assert.True(task != null);
            //Assert.True(task is Task);
            //Assert.False(task.GetType().IsGenericType);

            //await task;

            //Assert.True(instance.IsCompleted);
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

        
        public void TimeoutPresentOnInterfaceMethod_OverridesDefaultTimeout()
        {
            // TODO
        }

        
        public void TimeoutPresentOnImplementationMethod_IsIgnored()
        {
            // TODO
        }

        [Fact]
        public void WithNullableCancellationTokenParameter_NonNullCancellationToken_DoesntReplace()
        {
            const string expected = "quux";
            var instance = new Cancellable(expected);
            var proxy = CommandInterceptor.CreateProxy<ICancellable>(instance);

            var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(10000)); // Not 500, which is the value in the [Command] attribute.
            var token = source.Token;

            proxy.NullableCancellationTokenOnly(token);

            Assert.Equal(token, instance.ReceivedToken);
        }

        [Fact]
        public void WithNullableCancellationTokenParameter_CancellationTokenIsNull_UsesTokenFromExecuteAsync()
        {
            const string expected = "quux";
            var instance = new Cancellable(expected);
            var proxy = CommandInterceptor.CreateProxy<ICancellable>(instance);

            proxy.NullableCancellationTokenOnly(null);

            Assert.NotNull(instance.ReceivedToken);
            Assert.False(instance.ReceivedToken.Value.IsCancellationRequested);
            Thread.Sleep(Cancellable.CancellationTestTimeoutMillis + 10);
            Assert.True(instance.ReceivedToken.Value.IsCancellationRequested);
        }

        [Fact]
        public void WithNullableCancellationTokenParameter_CancellationTokenIsEmpty_UsesTokenFromExecuteAsync()
        {
            const string expected = "quux";
            var instance = new Cancellable(expected);
            var proxy = CommandInterceptor.CreateProxy<ICancellable>(instance);

            proxy.NullableCancellationTokenOnly(CancellationToken.None);

            Assert.NotNull(instance.ReceivedToken);
            Assert.False(instance.ReceivedToken.Value.IsCancellationRequested);
            Thread.Sleep(Cancellable.CancellationTestTimeoutMillis + 10);
            Assert.True(instance.ReceivedToken.Value.IsCancellationRequested);
        }

        [Fact]
        public void WithNonNullableCancellationTokenParameter_NonEmptyCancellationToken_DoesntReplace()
        {
            const string expected = "quux";
            var instance = new Cancellable(expected);
            var proxy = CommandInterceptor.CreateProxy<ICancellable>(instance);

            var source = new CancellationTokenSource(TimeSpan.FromMilliseconds(10000)); // Not 500, which is the value in the [Command] attribute.
            var token = source.Token;

            proxy.CancellationTokenOnly(token);

            Assert.Equal(token, instance.ReceivedToken);
        }

        [Fact]
        public void WithNonNullableCancellationTokenParameter_CancellationTokenIsEmpty_UsesTokenFromExecuteAsync()
        {
            const string expected = "quux";
            var instance = new Cancellable(expected);
            var proxy = CommandInterceptor.CreateProxy<ICancellable>(instance);

            proxy.CancellationTokenOnly(CancellationToken.None);

            Assert.NotNull(instance.ReceivedToken);
            Assert.False(instance.ReceivedToken.Value.IsCancellationRequested);
            Thread.Sleep(Cancellable.CancellationTestTimeoutMillis + 10);
            Assert.True(instance.ReceivedToken.Value.IsCancellationRequested);
        }

        [Fact]
        public void SlowSleepMethod_FireAndForget_ReturnsImmediatelyButStillCompletes()
        {
            var instance = new Sleepy();
            var proxy = CommandInterceptor.CreateProxy<ISleepy>(instance);

            var stopwatch = Stopwatch.StartNew();
            proxy.SleepWithFireAndForget(500);
            stopwatch.Stop();

            // Should have returned immediately; for sure before the sleep time.
            Assert.True(stopwatch.Elapsed.TotalMilliseconds < 400);
            Assert.False(instance.IsCompleted);

            Thread.Sleep(510);
            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public void SlowSleepMethod_WithoutFireAndForget_BlocksUntilCompletion()
        {
            var instance = new Sleepy();
            var proxy = CommandInterceptor.CreateProxy<ISleepy>(instance);

            var stopwatch = Stopwatch.StartNew();
            proxy.SleepWithoutFireAndForget(500);
            stopwatch.Stop();
            
            Assert.True(stopwatch.Elapsed.TotalMilliseconds > 500);
            Assert.True(instance.IsCompleted);
        }

        [Fact]
        public void FireAndForget_WhenThrows_DoesntThrowExceptionToCaller()
        {
            // The default TaskScheduler doesn't guarantee execution on another thread,
            // so we might get the exception back here. We catch and log in the interceptor
            // to avoid that.

            var instance = new ThrowingFireAndForget();
            var proxy = CommandInterceptor.CreateProxy<IThrowingFireAndForget>(instance);

            proxy.ImmediatelyThrowWithFireAndForget();

            Assert.True(true); // Expecting no exception to be thrown.
        }

        [Fact]
        public void CreateProxy_NonInterface_ThrowsInvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => {
                CommandInterceptor.CreateProxy(typeof (TestImplementation));
            });

            Assert.Equal("Proxies may only be created for interfaces", ex.Message);
        }

        [Fact]
        public void InterceptorCreateCommand_CommandName_IsInterfaceNameAndMethodName()
        {
            var method = typeof (ITestInterfaceWithImplementation).GetMethod("InvokeString");
            var mockInvocation = new Mock<IInvocation>();
            mockInvocation.SetupGet(m => m.Method).Returns(method);

            var command = new CommandInterceptor().CreateCommand<string>(mockInvocation.Object);

            Assert.Equal("foo.ITestInterfaceWithImplementation-InvokeString", command.Name);
        }

        // TODO How do we test for an unhandled exception thrown by a FireAndForget on a different thread?

        private ITestInterfaceWithImplementation CreateForImplementation(ITestInterfaceWithImplementation instance)
        {
            return CommandInterceptor.CreateProxy(instance);
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

    internal class TestThrowingImplementation : ITestInterfaceWithImplementation
    {
        private readonly ExpectedTestException _exceptionToThrow;

        public TestThrowingImplementation(ExpectedTestException exceptionToThrow)
        {
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<string> InvokeGenericTask()
        {
            throw _exceptionToThrow;
        }

        public Task<object> InvokeGenericTaskWithRunAndSleep()
        {
            return Task.Run((Func<object>)(() =>
            {
                Thread.Sleep(100);
                throw _exceptionToThrow;
            }));
        }

        public async Task<object> InvokeGenericTaskWithAwaitDelay()
        {
            await Task.Delay(100);
            throw _exceptionToThrow;
        }

        // Not supported.
        public Task InvokeUntypedTask()
        {
            throw new NotImplementedException();
        }

        public string InvokeString()
        {
            throw _exceptionToThrow;
        }

        public void InvokeVoid()
        {
            throw _exceptionToThrow;
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

    [Command("foo", "bar", 5000)]
    public interface ISleepy
    {
        [FireAndForget]
        void SleepWithFireAndForget(int sleepMillis);

        void SleepWithoutFireAndForget(int sleepMillis);
    }

    public class Sleepy : ISleepy
    {
        public bool IsCompleted { get; private set; }

        public Sleepy()
        {
            IsCompleted = false;
        }

        public void SleepWithFireAndForget(int sleepMillis)
        {
            Thread.Sleep(sleepMillis);
            IsCompleted = true;
        }

        public void SleepWithoutFireAndForget(int sleepMillis)
        {
            Thread.Sleep(sleepMillis);
            IsCompleted = true;
        }
    }

    [Command("foo", "bar", 10000)]
    public interface IThrowingFireAndForget
    {
        [FireAndForget]
        void ImmediatelyThrowWithFireAndForget();
    }

    public class ThrowingFireAndForget : IThrowingFireAndForget
    {
        public void ImmediatelyThrowWithFireAndForget()
        {
            throw new ExpectedTestException("Thrown from FireAndForget method");
        }
    }

    // These use a short timeout so that we can verify we're using the right CancellationToken.
    // The assumption here is that the default timeout is 10 or 15 seconds, and wouldn't be hit
    // by the time the test finishes and we assert. Not ideal, but there's not really a good
    // way to compare the token we get to the one that's created within the Command.
    [Command("foo", "bar", Cancellable.CancellationTestTimeoutMillis)]
    public interface ICancellable
    {
        string NullableCancellationTokenOnly(CancellationToken? token);
        string CancellationTokenOnly(CancellationToken token);
    }

    public class Cancellable : ICancellable
    {
        public const int CancellationTestTimeoutMillis = 500;

        private readonly string _returnValue;
        public CancellationToken? ReceivedToken { get; private set; }

        public Cancellable(string returnValue)
        {
            _returnValue = returnValue;
        }

        public string NullableCancellationTokenOnly(CancellationToken? token = null)
        {
            ReceivedToken = token;
            return _returnValue;
        }

        public string CancellationTokenOnly(CancellationToken token)
        {
            ReceivedToken = token;
            return _returnValue;
        }
    }
}