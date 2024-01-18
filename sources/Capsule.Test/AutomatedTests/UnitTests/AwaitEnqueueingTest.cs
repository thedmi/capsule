using Capsule.Extensions.DependencyInjection;

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
        await TestSuccessful(s => s.ExecuteInnerValueTaskAsync().AsTask());
    }

    private static async Task TestSuccessful(Func<IAwaitEnqueueingTestSubject, Task> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await hostedService.StartAsync(CancellationToken.None);

        var methodRunStarted = false;

        var sut =
            new AwaitEnqueueingTestSubject(tcs.Task, () => { methodRunStarted = true; }).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);

        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
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
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_throws()
    {
        await TestException(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_throws_value_task()
    {
        await TestException(s => s.ExecuteInnerValueTaskAsync().AsTask());
    }

    private static async Task TestException(Func<IAwaitEnqueueingTestSubject, Task> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        var exception = new InvalidOperationException("oh no");
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new AwaitEnqueueingTestSubject(tcs.Task, () => throw exception).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);

        await Task.Delay(100);
        
        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
        
        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_is_cancelled()
    {
        await TestCancellation(s => s.ExecuteInnerAsync());
    }

    [Test]
    public async Task Await_enqueueing_does_not_throw_exception_when_capsule_method_is_cancelled_value_task()
    {
        await TestCancellation(s => s.ExecuteInnerValueTaskAsync().AsTask());
    }

    private static async Task TestCancellation(Func<IAwaitEnqueueingTestSubject, Task> testSubjectCall)
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new AwaitEnqueueingTestSubject(tcs.Task, () => { }).Encapsulate(runtimeContext);

        var sutInvocationTask = testSubjectCall(sut);
        tcs.SetCanceled();

        await Task.Delay(100);
        
        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
        
        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }
}
