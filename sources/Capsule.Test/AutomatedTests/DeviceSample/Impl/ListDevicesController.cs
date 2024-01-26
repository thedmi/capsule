
namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public class ListDevicesController
{
    private readonly IDeviceRepository _repository;

    public ListDevicesController(IDeviceRepository repository)
    {
        _repository = repository;
    }

    public async Task<string[]> GetDevicesAsync()
    {
        var devices = await _repository.GetDevicesAsync();

        return devices.Select(device => device.Id.Value).ToArray();
    }
}
