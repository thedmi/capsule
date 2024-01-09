using CommunityToolkit.Diagnostics;

namespace Capsule.Test.AutomatedTests.Sample.Impl;

[Capsule]
public class DeviceLifecycleCoordinator : IDeviceRepository, ICapsule
{
    private readonly IReadOnlyList<WagoDeviceCapsuleFactory> _deviceFactories;

    private IReadOnlyList<IWagoDevice>? _devices;

    public DeviceLifecycleCoordinator(IReadOnlyList<WagoDeviceCapsuleFactory> deviceFactories)
    {
        _deviceFactories = deviceFactories;
    }

    [Expose]
    public async Task<IReadOnlyList<IWagoDevice>> GetDevicesAsync()
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
        _devices = _deviceFactories.Select(f => f.CreateCapsule()).ToList();
    }
    
}
