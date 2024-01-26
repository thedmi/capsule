
namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public interface IDeviceRepository
{
    Task<IReadOnlyList<IDevice>> GetDevicesAsync();
}
