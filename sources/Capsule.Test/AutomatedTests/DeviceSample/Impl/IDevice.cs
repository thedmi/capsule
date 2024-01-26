namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public interface IDevice : IAsyncDisposable
{
    public DeviceId Id { get; }

    public Task<bool> SetChannelStateAsync(ChannelId id, bool value);

    public Task<IReadOnlyDictionary<ChannelId, bool>> CurrentChannelStatesAsync();

    public Task TriggerForcedUpdateAsync(CancellationToken cancellationToken);
}
