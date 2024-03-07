using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule]
public class AwaitCompletionTestSubject(Task innerTask, Func<int> innerFunc)
{
    [Expose]
    public async Task<int> ExecuteInnerAsync()
    {
        await innerTask;
        return innerFunc();
    }
    
    [Expose(Synchronization = CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed)]
    public async Task<int> ExecuteWithFallbackAsync()
    {
        await innerTask;
        return innerFunc();
    }

    [Expose]
    public async ValueTask<int> ExecuteInnerValueTaskAsync()
    {
        return await ExecuteInnerAsync();
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed)]
    public async ValueTask<int> ExecuteValueTaskWithFallbackAsync()
    {
        return await ExecuteInnerAsync();
    }

    [Expose]
    public async Task<bool> SucceedAlwaysAsync()
    {
        return true;
    }
}
