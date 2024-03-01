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

    [Expose]
    public async ValueTask<int> ExecuteInnerValueTaskAsync()
    {
        return await ExecuteInnerAsync();
    }

    [Expose]
    public async Task<bool> SucceedAlwaysAsync()
    {
        return true;
    }
}
