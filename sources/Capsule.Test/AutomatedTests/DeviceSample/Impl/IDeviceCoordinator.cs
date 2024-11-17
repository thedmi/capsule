namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

public interface IDeviceCoordinator
{
    Task<IReadOnlyList<IDevice>> GetDevicesAsync();
}
