using Microsoft.Extensions.Logging;

namespace Senja.Test.Sample.Impl;

public class StateTrackerActor : IStateTracker, IActor
{
    private readonly ILogger<StateTrackerActor> _logger;

    public StateTrackerActor(ILogger<StateTrackerActor> logger)
    {
        _logger = logger;
    }

    public async Task TrackStatesAsync(IReadOnlyList<ChannelStateUpdate> update, CancellationToken cancellationToken)
    {
        _logger.LogInformation("State tracker actor called with {Count} updates", update.Count);
    }

    public async Task InitializeAsync()
    {
    }
}
