namespace Senja.Test.Sample.Impl;

public class StateTrackerActorFactory : ActorFactory<IStateTracker, StateTrackerActor>
{
    private readonly Func<StateTrackerActor> _implementationFactory;

    public StateTrackerActorFactory(Func<StateTrackerActor> implementationFactory, IActorHost actorHost) : base(actorHost)
    {
        _implementationFactory = implementationFactory;
    }

    protected override StateTrackerActor CreateImplementation() => _implementationFactory();

    protected override IStateTracker CreateFacade(StateTrackerActor implementation, IActorProxy actorProxy) =>
        new StateTrackerActorFacade(implementation, actorProxy);
}
