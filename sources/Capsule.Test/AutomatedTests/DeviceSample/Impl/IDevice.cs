namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public interface IDevice : IAsyncDisposable
{
    public string Id { get; }

    public Task<bool> SetOutputAsync(string channelId, bool value);

    public Task<IReadOnlyDictionary<string, bool>> ReadIosAsync();
}
