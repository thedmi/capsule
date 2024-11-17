using Capsule.Attribution;
using Capsule.GenericHosting;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class ConcurrencyTest
{
    [Test]
    public async Task Capsule_does_not_produce_inconsistent_state_when_under_concurrent_load()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>()
        );

        var sut = new ConcurrencyTestSubject().Encapsulate(runtimeContext);

        var taskRes1 = sut.IncrementAwaitResultAsync();
        var taskRes2 = sut.IncrementAwaitResultAsync();
        sut.IncrementAwaitEnqueueingAsync();
        var taskRec1 = sut.IncrementAwaitReceptionAsync();
        sut.IncrementAwaitEnqueueingAsync();
        var taskRec2 = sut.IncrementAwaitReceptionAsync();
        var taskRes3 = sut.IncrementAwaitResultAsync();
        var taskRes4 = sut.IncrementAwaitResultAsync();

        sut.GetStateUnsafe().ShouldBe(0);

        await hostedService.StartAsync(CancellationToken.None);
        await Task.WhenAll(taskRes1, taskRes2, taskRes3, taskRes4, taskRec1, taskRec2);

        taskRes1.Result.ShouldBe(11);
        taskRes2.Result.ShouldBe(12);

        taskRes3.Result.ShouldBe(17);
        taskRes4.Result.ShouldBe(18);

        await hostedService.StopAsync(CancellationToken.None);
        await hostedService.ExecuteTask!;
    }
}

[Capsule]
public class ConcurrencyTestSubject : CapsuleFeature.IInitializer
{
    private int _someState;

    public async Task InitializeAsync()
    {
        _someState = 10;
    }

    [Expose]
    public async Task<int> IncrementAwaitResultAsync()
    {
        await IncrementAsync();

        return _someState;
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitReception)]
    public async Task IncrementAwaitReceptionAsync()
    {
        await IncrementAsync();
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
    public async Task IncrementAwaitEnqueueingAsync()
    {
        await IncrementAsync();
    }

    [Expose(Synchronization = CapsuleSynchronization.PassThrough)]
    public int GetStateUnsafe()
    {
        return _someState;
    }

    private async Task IncrementAsync()
    {
        var i = _someState;
        await Task.Delay(100);
        _someState = i + 1;
    }
}
