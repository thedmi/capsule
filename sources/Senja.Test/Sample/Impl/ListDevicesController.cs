namespace Senja.Test.Sample.Impl;

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

        var result = new List<DeviceId>();

        foreach (var device in devices)
        {
            result.Add(device.GetId());
        }

        return result.Select(i => i.Value).ToArray();
    }
}
