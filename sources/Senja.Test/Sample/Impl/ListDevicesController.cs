namespace Senja.Test.Sample.Impl;

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

        var result = new List<DeviceId>();

        foreach (var device in devices)
        {
            result.Add(await device.GetIdAsync());
        }

        return result.Select(i => i.Value).ToArray();
    }
}
