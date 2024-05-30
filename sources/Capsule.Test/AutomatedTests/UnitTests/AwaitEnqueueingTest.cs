using Capsule.Attribution;
using Capsule.GenericHosting;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class AwaitEnqueueingTest
{
    [Test]
    public async Task Await_enqueueing_returns_when_invocation_was_enqueued()
    {
        await TestSuccessful(s => s.ExecuteInnerAsync());
    }
    
    [Test]
    public async Task Await_enqueueing_returns_when_invocation_was_enqueued_value_task()
    {
        await TestSuccessful(s => s.ExecuteInnerValueTaskAsync());
    }
    
    [Test]
    public async Task Await_enqueueing_returns_when_invocation_was_enqueued_sync()
    {
        await TestSuccessful(s => s.ExecuteInnerSync());
    }

    private static async Task TestSuccessful(Action<IAwaitEnqueueingTestSubject> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await hostedService.StartAsync(CancellationToken.None);

        var methodRunStarted = false;

        var sut =
            new TestSubject(tcs.Task, () => { methodRunStarted = true; }).Encapsulate(runtimeContext);

        testSubjectCall(sut);

        methodRunStarted.ShouldBeFalse();
        
        var awaitCompletionTask = sut.SucceedAlwaysAsync();
        await Task.Delay(100);
        
        methodRunStarted.ShouldBeTrue();
        
        // The second invocation, this time with await completion, should still be pending because the first one hasn't
        // finished yet
        awaitCompletionTask.Status.ShouldBe(TaskStatus.WaitingForActivation);
        
        // Now let the first invocation finish
        tcs.SetResult();
        await Task.Delay(100);
        
        awaitCompletionTask.IsCompleted.ShouldBeTrue();
        awaitCompletionTask.IsCompletedSuccessfully.ShouldBeTrue();

        (await awaitCompletionTask).ShouldBeTrue();
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_throws_but_host_throws()
    {
        await TestException(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_throws_but_host_throws_value_task()
    {
        await TestException(s => s.ExecuteInnerValueTaskAsync());
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_throws_but_host_throws_sync()
    {
        await TestException(s => s.ExecuteInnerSync());
    }

    private static async Task TestException(Action<IAwaitEnqueueingTestSubject> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        var exception = new InvalidOperationException("oh no");
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new TestSubject(tcs.Task, () => throw exception).Encapsulate(runtimeContext);

        testSubjectCall(sut);

        await Task.Delay(100);
        
        // Ensure that the loop has been terminated
        await Should.ThrowAsync<CapsuleInvocationException>(async () => await sut.SucceedAlwaysAsync());
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await Should.ThrowAsync<InvalidOperationException>(async () => await hostedService.ExecuteTask);
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_is_cancelled_but_host_throws()
    {
        await TestCancellation(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_is_cancelled_but_host_throws_value_task()
    {
        await TestCancellation(s => s.ExecuteInnerValueTaskAsync());
    }

    private static async Task TestCancellation(Action<IAwaitEnqueueingTestSubject> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new TestSubject(tcs.Task, () => { }).Encapsulate(runtimeContext);

        testSubjectCall(sut);
        tcs.SetCanceled();

        await Task.Delay(100);
        
        // Ensure that the loop has been terminated
        await Should.ThrowAsync<CapsuleInvocationException>(async () => await sut.SucceedAlwaysAsync());
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await Should.ThrowAsync<OperationCanceledException>(async () => await hostedService.ExecuteTask);
    }
    
    [Capsule(InterfaceName = "IAwaitEnqueueingTestSubject")]
    public class TestSubject(Task innerTask, Action preAction)
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task ExecuteInnerAsync()
        {
            preAction();
            await innerTask;
        }

        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async ValueTask ExecuteInnerValueTaskAsync()
        {
            await ExecuteInnerAsync();
        }

        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void ExecuteInnerSync()
        {
            preAction();
            innerTask.Wait();
        }

        [Expose]
        public async Task<bool> SucceedAlwaysAsync()
        {
            return true;
        }
    }

}
