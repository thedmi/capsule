using Capsule.Extensions.DependencyInjection;
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
                c.AddNUnit();
                c.SetMinimumLevel(LogLevel.Debug);
            });
        
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>());
        var backgroundService = new CapsuleBackgroundService(host);
        var runtimeContext = new CapsuleRuntimeContext(host,
            new CapsuleInvocationLoopFactory(loggerFactory.CreateLogger<CapsuleInvocationLoop>()));

        await backgroundService.StartAsync(CancellationToken.None);

        var stateTrackerFactory = new StateTrackerCapsuleFactory(
            () => new StateTracker(loggerFactory.CreateLogger<StateTracker>()),
            runtimeContext);

        var stateTracker = stateTrackerFactory.CreateCapsule();

        var wago1Factory = new WagoDeviceCapsuleFactory(
            () => new WagoDevice(
                new DeviceId("wago-1"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDevice>()),
            runtimeContext);

        var wago2Factory = new WagoDeviceCapsuleFactory(
            () => new WagoDevice(
                new DeviceId("wago-2"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDevice>()),
            runtimeContext);

        var coordinator = new DeviceLifecycleCoordinatorCapsuleFactory(
            () => new DeviceLifecycleCoordinator([wago1Factory, wago2Factory]),
            runtimeContext).CreateCapsule();

        var controller = new ListDevicesController(coordinator);
        
        Console.WriteLine("Started");

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await backgroundService.StopAsync(CancellationToken.None);
    }

}
