using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule]
public class AwaitEnqueueingTestSubject
{
    private readonly Task _innerTask;
    private readonly Action _preAction;

    public AwaitEnqueueingTestSubject(Task innerTask, Action preAction)
    {
        _innerTask = innerTask;
        _preAction = preAction;
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitEnqueueing)]
    public async Task ExecuteInnerAsync()
    {
        _preAction();
        await _innerTask;
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
