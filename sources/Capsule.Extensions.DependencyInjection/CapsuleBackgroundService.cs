using Microsoft.Extensions.Hosting;

namespace Capsule.Extensions.DependencyInjection;

/// <summary>
/// A hosted service that contains an <see cref="ICapsuleHost"/> and shuts it down when application shutdown is
/// initiated.
/// </summary>
public class CapsuleBackgroundService(CapsuleHost host) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await host.RunAsync(stoppingToken).ConfigureAwait(false);
    }
}
