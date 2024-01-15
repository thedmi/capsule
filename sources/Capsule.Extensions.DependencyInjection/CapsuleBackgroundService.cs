using Microsoft.Extensions.Hosting;

namespace Capsule.Extensions.DependencyInjection;

public class CapsuleBackgroundService : BackgroundService
{
    private readonly CapsuleHost _host;

    public CapsuleBackgroundService(CapsuleHost host)
    {
        _host = host;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _host.RunAsync(stoppingToken);
    }
}
