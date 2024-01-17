using Capsule.Attribution;

using CommunityToolkit.Diagnostics;

namespace Capsule.Test.AutomatedTests.Sample.Impl;

[Capsule]
public class DeviceLifecycleCoordinator : IDeviceRepository, ICapsule
{
    private readonly Func<IReadOnlyList<IDevice>> _deviceFactories;

    private IReadOnlyList<IDevice>? _devices;

    public DeviceLifecycleCoordinator(Func<IReadOnlyList<IDevice>> deviceFactories)
    {
        _deviceFactories = deviceFactories;
    }

    [Expose]
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
        _devices = _deviceFactories();
    }
    
}
