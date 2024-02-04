
namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public class ListDevicesController
{
    private readonly IDeviceCoordinator _coordinator;

    public ListDevicesController(IDeviceCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public async Task<string[]> GetDevicesAsync()
    {
        var devices = await _coordinator.GetDevicesAsync();

        return devices.Select(device => device.Id).ToArray();
    }
}
