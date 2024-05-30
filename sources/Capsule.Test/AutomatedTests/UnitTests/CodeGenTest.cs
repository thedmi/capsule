using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

// This file contains various constructs that affect code generation and are here to verify that the generated code
// does actually compile.

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
        
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }
    }

    [Capsule]
    public class WithPredefinedInterface : IWithPredefinedInterface
    {
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public void SyncMethod() { }
        
        [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
        public async Task AsyncMethod() { }
    }
    
}

public interface IWithPredefinedInterface
{
    void SyncMethod();

    Task AsyncMethod();
}
