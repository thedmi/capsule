using Capsule.Extensions.DependencyInjection;

using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class AwaitCompletionTest
{
    [Test]
    public async Task Await_completion_returns_result_when_method_ran_to_completion_successfully()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        await hostedService.StartAsync(CancellationToken.None);

        var factory =
            new AwaitCompletionTestSubjectCapsuleFactory(() => new AwaitCompletionTestSubject(tcs.Task, () => 42),
                runtimeContext);
        
        var sut = factory.CreateCapsule();

        var sutInvocationTask = sut.ExecuteInnerAsync();

        await Task.Yield();
        
        sutInvocationTask.IsCompleted.ShouldBeFalse();
        
        tcs.SetResult();
        await sutInvocationTask;
        
        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
        sutInvocationTask.Result.ShouldBe(42);
        
        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }
    
    [Test]
    public async Task Await_completion_throws_when_method_ran_to_completion_with_exception()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        var exception = new InvalidOperationException("oh no");
        
        await hostedService.StartAsync(CancellationToken.None);

        var factory =
            new AwaitCompletionTestSubjectCapsuleFactory(
                () => new AwaitCompletionTestSubject(tcs.Task, () => throw exception), runtimeContext);
        
        var sut = factory.CreateCapsule();

        var sutInvocationTask = sut.ExecuteInnerAsync();

        await Task.Yield();
        
        sutInvocationTask.IsCompleted.ShouldBeFalse();
        
        tcs.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(async () => await sutInvocationTask);
        
        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsFaulted.ShouldBeTrue();
        sutInvocationTask.Exception!.InnerExceptions.ShouldBe(new [] { exception });

        // Ensure that the loop is still active and is able to handle a second invocation
        (await sut.SucceedAlwaysAsync()).ShouldBeTrue();
        
        await hostedService.StopAsync(CancellationToken.None);
        await Task.Delay(100);
        await hostedService.ExecuteTask!;
    }
    
    [Test]
    public async Task Await_completion_throws_when_method_is_cancelled()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService((CapsuleHost)runtimeContext.Host);
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        await hostedService.StartAsync(CancellationToken.None);

        var factory =
            new AwaitCompletionTestSubjectCapsuleFactory(() => new AwaitCompletionTestSubject(tcs.Task, () => 1),
                runtimeContext);
        
        var sut = factory.CreateCapsule();

        var sutInvocationTask = sut.ExecuteInnerAsync();

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
}
