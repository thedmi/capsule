namespace Senja.Test.Sample.Impl;

public class WagoDeviceActorFacade : IDevice
{
    private readonly IDevice _impl;

    private readonly IActorProxy _proxy;

    public WagoDeviceActorFacade(IDevice impl, IActorProxy proxy)
    {
        _impl = impl;
        _proxy = proxy;
    }

    public Task<DeviceId> GetIdAsync() => _proxy.EnqueueAwaitResult(() => _impl.GetIdAsync());

    public Task<bool> SetChannelStateAsync(ChannelId id, bool value) =>
        _proxy.EnqueueAwaitResult(() => _impl.SetChannelStateAsync(id, value));

    public Task<IReadOnlyDictionary<ChannelId, bool>> CurrentChannelStatesAsync() =>
        _proxy.EnqueueAwaitResult(() => _impl.CurrentChannelStatesAsync());

    public Task TriggerForcedUpdateAsync(CancellationToken cancellationToken) =>
        _proxy.EnqueueAwaitReception(() => _impl.TriggerForcedUpdateAsync(cancellationToken));
}
