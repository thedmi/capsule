namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public class ListDevicesController(IDeviceCoordinator coordinator)
{
    public async Task<string[]> GetDevicesAsync()
    {
        var devices = await coordinator.GetDevicesAsync();

        return devices.Select(device => device.Id).ToArray();
    }
}
