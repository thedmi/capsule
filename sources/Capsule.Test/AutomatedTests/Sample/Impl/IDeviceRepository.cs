
namespace Capsule.Test.AutomatedTests.Sample.Impl;

public interface IDeviceRepository
{
    Task<IReadOnlyList<IDevice>> GetDevicesAsync();
}
