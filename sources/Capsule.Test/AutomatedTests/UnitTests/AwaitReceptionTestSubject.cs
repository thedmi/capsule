using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule(InterfaceName = "ISomeAwaitReceptionTestSubject")]
public class AwaitReceptionTestSubject(Task innerTask, Action preAction)
{
    [Expose(Synchronization = CapsuleSynchronization.AwaitReception)]
    public async Task ExecuteInnerAsync()
    {
        preAction();
        await innerTask;
    }

    [Expose]
    public async Task<bool> SucceedAlwaysAsync()
    {
        return true;
    }
}
