namespace Capsule;

public interface ICapsuleSynchronizerFactory
{
    /// <summary>
    /// Creates a synchronizer and its invocation loop for <paramref name="capsuleImpl"/>. The invocation loop is
    /// registered with the <paramref name="host"/>, the synchronizer is returned.
    /// </summary>
    ICapsuleSynchronizer Create(object capsuleImpl, ICapsuleHost host);
}
