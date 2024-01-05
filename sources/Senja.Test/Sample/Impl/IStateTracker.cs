namespace Senja.Test.Sample.Impl;

public interface IStateTracker
{
    [EnqueueAwaitResult]
    Task TrackStatesAsync(IReadOnlyList<ChannelStateUpdate> update, CancellationToken cancellationToken);
}
