﻿using Capsule.Extensions.DependencyInjection;
using Capsule.Test.AutomatedTests.Sample.Impl;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.Sample;

public class CapsuleExample
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

                    services.AddSingleton<IWagoDevice>(
                        p => ActivatorUtilities.CreateInstance<WagoDevice>(p, new DeviceId("wago1"))
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<IWagoDevice>(
                        p => ActivatorUtilities.CreateInstance<WagoDevice>(p, new DeviceId("wago2"))
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<StateTracker>();
                    services.AddSingleton<IStateTracker>(
                        p => p.GetRequiredService<StateTracker>()
                            .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));

                    services.AddSingleton<IDeviceLifecycleCoordinator>(
                        p => new DeviceLifecycleCoordinator(() => p.GetServices<IWagoDevice>().ToList()).Encapsulate(
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
