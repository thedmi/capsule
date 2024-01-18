using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

[Capsule]
public class AwaitCompletionTestSubject : ICapsule
{
    private readonly Task _innerTask;
    private readonly Func<int> _innerFunc;

    public AwaitCompletionTestSubject(Task innerTask, Func<int> innerFunc)
    {
        _innerTask = innerTask;
        _innerFunc = innerFunc;
    }

    public async Task InitializeAsync() { }

    [Expose]
    public async Task<int> ExecuteInnerAsync()
    {
        await _innerTask;
        return _innerFunc();
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
