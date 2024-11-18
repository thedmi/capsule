using Capsule.Attribution;
using Capsule.GenericHosting;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class AwaitCompletionTest
{
    [TestCase(true)]
    [TestCase(false)]
    public async Task Await_completion_returns_result_when_method_ran_to_completion_successfully(bool withFallback)
    {
        await TestSuccessfulCompletion(s => withFallback ? s.ExecuteWithFallbackAsync() : s.ExecuteInnerAsync());
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task Await_completion_returns_result_when_method_ran_to_completion_successfully_value_task(
        bool withFallback
    )
    {
        await TestSuccessfulCompletion(s =>
            withFallback ? s.ExecuteValueTaskWithFallbackAsync().AsTask() : s.ExecuteInnerValueTaskAsync().AsTask()
        );
    }

    private static async Task TestSuccessfulCompletion(Func<IAwaitCompletionTestSubject, Task<int>> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>()
        );

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await hostedService.StartAsync(CancellationToken.None);

        var sut = new TestSubject(tcs.Task, () => 42).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);

        await Task.Yield();

        sutInvocationTask.IsCompleted.ShouldBeFalse();

        tcs.SetResult();
        await sutInvocationTask;

        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
#pragma warning disable VSTHRD103
        sutInvocationTask.Result.ShouldBe(42);
#pragma warning restore VSTHRD103

        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();

        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }

    [Test]
    public async Task Await_completion_throws_when_method_ran_to_completion_with_exception()
    {
        await TestException(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_completion_throws_when_method_ran_to_completion_with_exception_value_task()
    {
        await TestException(s => s.ExecuteInnerValueTaskAsync().AsTask());
    }

    private static async Task TestException(Func<IAwaitCompletionTestSubject, Task<int>> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>()
        );

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var exception = new InvalidOperationException("oh no");

        await hostedService.StartAsync(CancellationToken.None);

        var sut = new TestSubject(tcs.Task, () => throw exception).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);

        await Task.Yield();

        sutInvocationTask.IsCompleted.ShouldBeFalse();

        tcs.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(async () => await sutInvocationTask);

        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsFaulted.ShouldBeTrue();
        sutInvocationTask.Exception!.InnerExceptions.ShouldBe(new[] { exception });

        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();

        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }

    [Test]
    public async Task Await_completion_throws_when_method_is_cancelled()
    {
        await TestCancellation(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_completion_throws_when_method_is_cancelled_value_task()
    {
        await TestCancellation(s => s.ExecuteInnerValueTaskAsync().AsTask());
    }

    private static async Task TestCancellation(Func<IAwaitCompletionTestSubject, Task<int>> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>()
        );

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await hostedService.StartAsync(CancellationToken.None);

        var sut = new TestSubject(tcs.Task, () => 42).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);

        await Task.Yield();

        sutInvocationTask.IsCompleted.ShouldBeFalse();

        tcs.SetCanceled();
        await Should.ThrowAsync<OperationCanceledException>(async () => await sutInvocationTask);

        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCanceled.ShouldBeTrue();

        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();

        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }

    [Capsule(InterfaceName = "IAwaitCompletionTestSubject")]
    public class TestSubject(Task innerTask, Func<int> innerFunc)
    {
        [Expose]
        public async Task<int> ExecuteInnerAsync()
        {
            await innerTask;
            return innerFunc();
        }

        [Expose(Synchronization = CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed)]
        public async Task<int> ExecuteWithFallbackAsync()
        {
            await innerTask;
            return innerFunc();
        }

        [Expose]
        public async ValueTask<int> ExecuteInnerValueTaskAsync()
        {
            return await ExecuteInnerAsync();
        }

        [Expose(Synchronization = CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed)]
        public async ValueTask<int> ExecuteValueTaskWithFallbackAsync()
        {
            return await ExecuteInnerAsync();
        }

        [Expose]
        public async Task<bool> SucceedAlwaysAsync()
        {
            return true;
        }
    }
}
