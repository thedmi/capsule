using Capsule.Attribution;

using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

[Capsule]
public class StateTracker : ISomethingElse
{
    private readonly ILogger<StateTracker> _logger;

    public StateTracker(ILogger<StateTracker> logger)
    {
        _logger = logger;
    }

    [Expose]
    public async Task TrackStatesAsync(IReadOnlyList<(string Channel, bool Value)> update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("State tracker called with {Count} updates", update.Count);
    }
}
