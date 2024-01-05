namespace Senja.Test.Sample.Impl;

public class StateTrackerActorFacade : IStateTracker
{
    private readonly IStateTracker _impl;

    private readonly IActorProxy _actorProxy;

    public StateTrackerActorFacade(IStateTracker impl, IActorProxy actorProxy)
    {
        _impl = impl;
        _actorProxy = actorProxy;
    }

    public async Task TrackStatesAsync(IReadOnlyList<ChannelStateUpdate> update, CancellationToken cancellationToken) =>
        _actorProxy.EnqueueReturn(() => _impl.TrackStatesAsync(update, cancellationToken));
}
