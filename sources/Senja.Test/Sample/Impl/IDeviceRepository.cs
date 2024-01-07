namespace Senja.Test.Sample.Impl;

public interface IDeviceRepository
{
    Task<IReadOnlyList<IWagoDevice>> GetDevicesAsync();
}
