using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.Sample.Impl;

[Capsule]
public class WagoDevice : ICapsule
{
    private readonly DeviceId _id;

    private readonly IStateTracker _stateTracker;

    private readonly ILogger<WagoDevice> _logger;

    public WagoDevice(DeviceId id, IStateTracker stateTracker, ILogger<WagoDevice> logger)
    {
        _id = id;
        _stateTracker = stateTracker;
        _logger = logger;
    }

    [Expose(Synchronization = CapsuleSynchronization.PassThrough)]
    public DeviceId GetId()
    {
        return _id;
    }

    [Expose]
    public async Task<bool> SetChannelStateAsync(ChannelId id, bool value)
    {
        return true;
    }

    [Expose]
    public async Task<IReadOnlyDictionary<ChannelId, bool>> CurrentChannelStatesAsync()
    {
        return new Dictionary<ChannelId, bool>();
    }

    [Expose(Synchronization = CapsuleSynchronization.AwaitReception)]
    public async Task TriggerForcedUpdateAsync(CancellationToken cancellationToken)
    {
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Wago device actor {Id} initialized", _id.Value);
    }

    private void SomePrivateMethod()
    {
    }
}
