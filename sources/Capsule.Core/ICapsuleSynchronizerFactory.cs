namespace Capsule;

public interface ICapsuleSynchronizerFactory
{
    ICapsuleSynchronizer Create(object capsuleImpl, CapsuleRuntimeContext context);
}
