using CommunityToolkit.Diagnostics;

namespace Senja.Test.Sample.Impl;

public class DeviceLifecycleCoordinatorActor : IDeviceRepository, IActor
{
    private readonly IReadOnlyList<WagoDeviceActorFactory> _deviceFactories;

    private IReadOnlyList<IDevice>? _devices;

    public DeviceLifecycleCoordinatorActor(IReadOnlyList<WagoDeviceActorFactory> deviceFactories)
    {
        _deviceFactories = deviceFactories;
    }

    public async Task<IReadOnlyList<IDevice>> GetDevicesAsync()
    {
        if (_devices == null)
        {
            throw new InvalidOperationException("Devices have not yet been initialized.");
        }

        return _devices;
    }

    public async Task InitializeAsync()
    {
        Guard.IsNull(_devices);
        _devices = _deviceFactories.Select(f => f.CreateActor()).ToList();
    }
    
}
