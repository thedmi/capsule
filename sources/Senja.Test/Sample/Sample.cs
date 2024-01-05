using Microsoft.Extensions.Logging;
using Senja.Test.Sample.Impl;

namespace Senja.Test.Sample;

public class ActorExample
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
        
        var host = new ActorHost(loggerFactory.CreateLogger<ActorHost>());

        await host.StartAsync(CancellationToken.None);

        var stateTrackerFactory = new StateTrackerActorFactory(
            () => new StateTrackerActor(loggerFactory.CreateLogger<StateTrackerActor>()),
            host);

        var stateTracker = stateTrackerFactory.CreateActor();

        var wago1Factory = new WagoDeviceActorFactory(
            () => new WagoDeviceActor(
                new DeviceId("wago-1"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDeviceActor>()),
            host);

        var wago2Factory = new WagoDeviceActorFactory(
            () => new WagoDeviceActor(
                new DeviceId("wago-2"),
                stateTracker,
                loggerFactory.CreateLogger<WagoDeviceActor>()),
            host);

        var coordinator = new DeviceLifecycleCoordinatorActorFactory(
            () => new DeviceLifecycleCoordinatorActor([wago1Factory, wago2Factory]),
            host).CreateActor();

        var controller = new ListDevicesController(coordinator);
        
        Console.WriteLine("Started");

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await host.StopAsync(CancellationToken.None);
    }

}
