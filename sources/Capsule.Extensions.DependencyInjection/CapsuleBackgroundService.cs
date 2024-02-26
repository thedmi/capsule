using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Capsule.Extensions.DependencyInjection;

/// <summary>
/// A hosted service that contains an <see cref="ICapsuleHost"/> and shuts it down when application shutdown is
/// initiated.
/// </summary>
public class CapsuleBackgroundService(CapsuleHost host, ILogger<CapsuleBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Starting Capsule background service...");
        
        await host.RunAsync(stoppingToken).ConfigureAwait(false);
        
        logger.LogDebug("Capsule background service terminated");
    }
}
