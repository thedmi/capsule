using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule(InterfaceName = "ISomeAwaitReceptionTestSubject")]
public class AwaitReceptionTestSubject
{
    private readonly Task _innerTask;
    private readonly Action _preAction;

    public AwaitReceptionTestSubject(Task innerTask, Action preAction)
    {
        _innerTask = innerTask;
        _preAction = preAction;
    }

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
