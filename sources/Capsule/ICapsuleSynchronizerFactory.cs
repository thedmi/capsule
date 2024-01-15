namespace Capsule;

public interface ICapsuleSynchronizerFactory
{
    ICapsuleSynchronizer Create(ICapsule capsule, CapsuleRuntimeContext context);
}
