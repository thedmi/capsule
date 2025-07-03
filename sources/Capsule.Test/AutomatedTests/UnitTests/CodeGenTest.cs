using Capsule.Attribution;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

// This file contains various constructs that affect code generation and are here to verify that the generated code
// does actually compile.

public class CodeGenTest
{
    [Test]
    public async Task CapsuleSynchronization_AwaitEnqueuing_with_generated_interface_supports_sync_and_async_interfaces()
    {
        var sut = new SyncAsyncTest.WithGeneratedInterface().Encapsulate(TestRuntime.Create());

        await Should.NotThrowAsync(async () =>
        {
            sut.AsyncMethod();
            sut.SyncMethod();
        });
    }

    [Test]
    public async Task CapsuleSynchronization_AwaitEnqueuing_with_provided_interface_supports_sync_and_async_interfaces()
    {
        var sut = new SyncAsyncTest.WithProvidedInterface().Encapsulate(TestRuntime.Create());

        await Should.NotThrowAsync(async () =>
        {
            await sut.AsyncMethod();
            sut.SyncMethod();
        });
    }

    [Test]
    public async Task CapsuleSynchronization_AwaitEnqueuing_with_resolved_interface_supports_sync_and_async_interfaces()
    {
        var sut = new SyncAsyncTest.WithResolvedInterface().Encapsulate(TestRuntime.Create());

        await Should.NotThrowAsync(async () =>
        {
            await sut.AsyncMethod();
            sut.SyncMethod();
        });
    }

    [Test]
    public async Task CapsuleSynchronization_AwaitEnqueuing_with_resolved_and_additional_interface_supports_sync_and_async_interfaces()
    {
        var sut = new SyncAsyncTest.WithResolvedAndAdditionalInterface().Encapsulate(TestRuntime.Create());

        await Should.NotThrowAsync(async () =>
        {
            await sut.AsyncMethod();
            sut.SyncMethod();
        });
    }
}

// Verify that deeply nested types can be used as Capsule
public class NestedTypeTest
{
    public class Inner
    {
        [Capsule]
        public class CodeGenTest
        {
            [Expose]
            public async Task DoIt() { }
        }
    }
}

public class SyncAsyncTest
{
    [Capsule]
    public class WithGeneratedInterface
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void SyncMethod() { }

        // Generated async method will be synchronous
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }
    }

    [Capsule]
    public class WithResolvedInterface : IWithPredefinedInterface
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void SyncMethod() { }

        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }
    }

    [Capsule(InterfaceName = "IWithPredefinedInterface", InterfaceGeneration = CapsuleInterfaceGeneration.Disable)]
    public class WithProvidedInterface
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void SyncMethod() { }

        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }
    }

    [Capsule]
    public class WithResolvedAndAdditionalInterface : IWithPredefinedInterface, IDisposable
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void SyncMethod() { }

        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }

        public void Dispose()
        {
            // No op
        }
    }
}

public interface IWithPredefinedInterface
{
    void SyncMethod();

    Task AsyncMethod();
}
