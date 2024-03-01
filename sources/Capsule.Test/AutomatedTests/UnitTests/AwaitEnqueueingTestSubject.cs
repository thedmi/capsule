using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule]
public class AwaitEnqueueingTestSubject(Task innerTask, Action preAction)
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

    [Expose]
    public async Task<bool> SucceedAlwaysAsync()
    {
        return true;
    }
}
