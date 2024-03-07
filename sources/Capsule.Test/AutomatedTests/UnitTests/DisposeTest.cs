using Capsule.Attribution;
using Capsule.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Moq;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class DisposeTest
{
    /// <summary>
    /// This test reproduces a problem found with ASP.NET Core hosting, where DI-registered services that implement
    /// IAsyncDisposable are disposed by the DI container on shutdown. However, that happens *after* the
    /// <see cref="CapsuleBackgroundService"/> has already been shut down, so the dispose call is enqueued by never
    /// processed.
    /// </summary>
    [Test]
    public async Task Disposing_a_capsule_with_a_closed_invocation_queue_doesnt_deadlock_or_throw()
    {
        var runtimeContext = TestRuntime.Create();
        var hostedService = new CapsuleBackgroundService(
            (CapsuleHost)runtimeContext.Host,
            Mock.Of<ILogger<CapsuleBackgroundService>>());

        var sutImpl = new DisposeTestSubject();
        var sut = sutImpl.Encapsulate(runtimeContext);
        
        await hostedService.StartAsync(CancellationToken.None);

        await hostedService.StopAsync(CancellationToken.None);

        // Act & assert
        await sut.DisposeAsync();
    }
}

[Capsule]
public class DisposeTestSubject : IAsyncDisposable
{
    [Expose(Synchronization = CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed)]
    public async ValueTask DisposeAsync()
    {
    }
}
