namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule]
public class AwaitReceptionTestSubject : ICapsule
{
    private readonly Task _innerTask;
    private readonly Action _preAction;

    public AwaitReceptionTestSubject(Task innerTask, Action preAction)
    {
        _innerTask = innerTask;
        _preAction = preAction;
    }

    public async Task InitializeAsync() { }

    [Expose(Synchronization = CapsuleSynchronization.AwaitReception)]
    public async Task ExecuteInnerAsync()
    {
        _preAction();
        await _innerTask;
    }

    [Expose]
    public async Task<bool> SucceedAlwaysAsync()
    {
        return true;
    }
}
