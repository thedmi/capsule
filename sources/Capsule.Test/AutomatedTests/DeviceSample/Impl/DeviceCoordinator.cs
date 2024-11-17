using Capsule.Attribution;
using CommunityToolkit.Diagnostics;

namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

[Capsule]
public class DeviceCoordinator(Func<IReadOnlyList<IDevice>> deviceFactories)
    : IDeviceCoordinator,
        CapsuleFeature.IInitializer
{
    private IReadOnlyList<IDevice>? _devices;

    public async Task InitializeAsync()
    {
        Guard.IsNull(_devices);
        _devices = deviceFactories();
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
}
