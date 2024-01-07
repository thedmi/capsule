using Capsule.Test.AutomatedTests.Sample.Impl;
using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.Sample;

public class CapsuleExample
{
    [Test]
    public async Task RunAsync()
    {
        var loggerFactory = LoggerFactory.Create(
            c =>
            {
                c.AddConsole();
                c.SetMinimumLevel(LogLevel.Debug);
            });
        
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>());

        await host.StartAsync(CancellationToken.None);

        var stateTrackerFactory = new StateTrackerCapsuleFactory(
            () => new StateTracker(loggerFactory.CreateLogger<StateTracker>()),
            host);

        var stateTracker = stateTrackerFactory.CreateCapsule();

        var wago1Factory = new WagoDeviceCapsuleFactory(
            () => new WagoDevice(
                new DeviceId("wago-1"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDevice>()),
            host);

        var wago2Factory = new WagoDeviceCapsuleFactory(
            () => new WagoDevice(
                new DeviceId("wago-2"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDevice>()),
            host);

        var coordinator = new DeviceLifecycleCoordinatorCapsuleFactory(
            () => new DeviceLifecycleCoordinator([wago1Factory, wago2Factory]),
            host).CreateCapsule();

        var controller = new ListDevicesController(coordinator);
        
        Console.WriteLine("Started");

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await host.StopAsync(CancellationToken.None);
    }

}
