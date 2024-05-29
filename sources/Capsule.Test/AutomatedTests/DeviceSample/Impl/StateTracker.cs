using Capsule.Attribution;

using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

[Capsule]
public class StateTracker(ILogger<StateTracker> logger) : ISomethingElse
{
    [Expose]
    public async Task TrackStatesAsync(IReadOnlyList<(string Channel, bool Value)> update, CancellationToken cancellationToken)
    {
        logger.LogInformation("State tracker called with {Count} updates", update.Count);
    }
}
