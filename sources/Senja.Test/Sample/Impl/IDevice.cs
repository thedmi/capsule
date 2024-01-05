namespace Senja.Test.Sample.Impl;

public interface IDevice
{
    public Task<DeviceId> GetIdAsync();
    
    public Task<bool> SetChannelStateAsync(ChannelId id, bool value);
    
    Task<IReadOnlyDictionary<ChannelId, bool>> CurrentChannelStatesAsync();
    
    public Task TriggerForcedUpdateAsync(CancellationToken cancellationToken);
}
