namespace Capsule;

public abstract class CapsuleFactory<T, TImpl> : ICapsuleFactory<T> where TImpl : ICapsule
{
    private readonly CapsuleRuntimeContext _runtimeContext;
    
    protected CapsuleFactory(CapsuleRuntimeContext runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }
    
    public T CreateCapsule()
    {
        var implementation = CreateImplementation();

        var synchronizer = _runtimeContext.SynchronizerFactory.Create(implementation, _runtimeContext);
        
        return CreateFacade(implementation, synchronizer);
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, ICapsuleSynchronizer synchronizer);
}
