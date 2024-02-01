using Capsule.Attribution;

using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.DeviceSample.Impl;

[Capsule]
public class StateTracker : ISomethingElse, CapsuleFeature.IInitializer
{
    private readonly ILogger<StateTracker> _logger;

    public StateTracker(ILogger<StateTracker> logger)
    {
        _logger = logger;
    }

    [Expose]
    public async Task TrackStatesAsync(IReadOnlyList<ChannelStateUpdate> update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("State tracker actor called with {Count} updates", update.Count);
    }

    public async Task InitializeAsync()
    {
    }
}
