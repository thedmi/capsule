
namespace Capsule.Test.AutomatedTests.Sample.Impl;

public class ListDevicesController
{
    private readonly IDeviceLifecycleCoordinator _repository;

    public ListDevicesController(IDeviceLifecycleCoordinator repository)
    {
        _repository = repository;
    }

    public async Task<string[]> GetDevicesAsync()
    {
        var devices = await _repository.GetDevicesAsync();

        return devices.Select(device => device.Id.Value).ToArray();
    }
}
