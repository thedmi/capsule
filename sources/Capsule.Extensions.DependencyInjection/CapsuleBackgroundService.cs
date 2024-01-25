using Microsoft.Extensions.Hosting;

namespace Capsule.Extensions.DependencyInjection;

public class CapsuleBackgroundService(CapsuleHost host) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await host.RunAsync(stoppingToken);
    }
}
