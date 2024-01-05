namespace Senja.Test.Sample.Impl;

public interface IDeviceRepository
{
    Task<IReadOnlyList<IDevice>> GetDevicesAsync();
}
