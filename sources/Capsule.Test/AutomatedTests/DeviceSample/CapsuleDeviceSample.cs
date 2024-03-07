using Capsule.DependencyInjection;
using Capsule.Test.AutomatedTests.DeviceSample.Impl;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.DeviceSample;

public class CapsuleDeviceSample
{
    [Test]
    public async Task RunAsync()
    {
        using var app = ConfigureHost();

        var controller = app.Services.GetRequiredService<ListDevicesController>();
        
        await RunExample(controller);

        await app.StopAsync(CancellationToken.None);
        await Task.Delay(100);
    }

    private static IHost ConfigureHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(
                services =>
                {
                    services.AddLogging(
                        l =>
                        {
                            l.AddNUnit();
                            l.SetMinimumLevel(LogLevel.Debug);
                        });

                    services.AddCapsuleHost();

                    services.AddSingleton<IDevice>(
                        p => ActivatorUtilities.CreateInstance<FooDevice>(p, "foo1")
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<IDevice>(
                        p => ActivatorUtilities.CreateInstance<FooDevice>(p, "foo2")
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<StateTracker>();
                    services.AddSingleton<IStateTracker>(
                        p => p.GetRequiredService<StateTracker>()
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<IDeviceCoordinator>(
                        p => new DeviceCoordinator(() => p.GetServices<IDevice>().ToList()).Encapsulate(
                            p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddTransient<ListDevicesController>();
                })
            .Build();
    }

    private static async Task RunExample(ListDevicesController controller)
    {
        Console.WriteLine("Started");

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));

        await Task.Delay(500);
        
        Console.WriteLine(string.Join(", ", await controller.GetDevicesAsync()));
    }
}
