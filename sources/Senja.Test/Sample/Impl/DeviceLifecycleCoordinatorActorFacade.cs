namespace Senja.Test.Sample.Impl;

public class DeviceLifecycleCoordinatorActorFacade : IDeviceRepository
{
    private readonly IDeviceRepository _impl;

    private readonly IActorProxy _proxy;

    public DeviceLifecycleCoordinatorActorFacade(IDeviceRepository impl, IActorProxy proxy)
    {
        _impl = impl;
        _proxy = proxy;
    }

    public Task<IReadOnlyList<IDevice>> GetDevicesAsync() => _proxy.EnqueueAwaitResult(() => _impl.GetDevicesAsync());
}
