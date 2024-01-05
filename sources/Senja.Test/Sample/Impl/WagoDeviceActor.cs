using Microsoft.Extensions.Logging;

namespace Senja.Test.Sample.Impl;

public class WagoDeviceActor : IDevice, IActor
{
    private readonly DeviceId _id;

    private readonly IStateTracker _stateTracker;

    private readonly ILogger<WagoDeviceActor> _logger;

    public WagoDeviceActor(DeviceId id, IStateTracker stateTracker, ILogger<WagoDeviceActor> logger)
    {
        _id = id;
        _stateTracker = stateTracker;
        _logger = logger;
    }

    public async Task<DeviceId> GetIdAsync()
    {
        return _id;
    }

    public async Task<bool> SetChannelStateAsync(ChannelId id, bool value)
    {
        return true;
    }

    public async Task<IReadOnlyDictionary<ChannelId, bool>> CurrentChannelStatesAsync()
    {
        return new Dictionary<ChannelId, bool>();
    }

    public async Task TriggerForcedUpdateAsync(CancellationToken cancellationToken)
    {
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Wago device actor {Id} initialized", _id.Value);
    }
}
