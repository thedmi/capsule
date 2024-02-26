using Capsule.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class AwaitReceptionTest
{
    [Test]
    public async Task Await_reception_returns_when_invocation_was_started()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await hostedService.StartAsync(CancellationToken.None);

        var methodRunStarted = false;

        var sut = new AwaitReceptionTestSubject(tcs.Task, () => { methodRunStarted = true; }).Encapsulate(runtimeContext);

        var sutInvocationTask = sut.ExecuteInnerAsync();

        await Task.Delay(100);
        
        sutInvocationTask.IsCompleted.ShouldBeTrue();
        sutInvocationTask.IsCompletedSuccessfully.ShouldBeTrue();
        methodRunStarted.ShouldBeTrue();
        
        var awaitCompletionTask = sut.SucceedAlwaysAsync();
        await Task.Delay(100);
        
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
    public async Task Await_reception_does_not_throw_exception_when_capsule_method_throws()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        var exception = new InvalidOperationException("oh no");
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new AwaitReceptionTestSubject(tcs.Task, () => throw exception).Encapsulate(runtimeContext);

        var sutInvocationTask = sut.ExecuteInnerAsync();

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
    public async Task Await_reception_does_not_throw_exception_when_capsule_method_is_cancelled()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());
        
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        await hostedService.StartAsync(CancellationToken.None);

        var sut = new AwaitReceptionTestSubject(tcs.Task, () => { }).Encapsulate(runtimeContext);

        var sutInvocationTask = sut.ExecuteInnerAsync();
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
