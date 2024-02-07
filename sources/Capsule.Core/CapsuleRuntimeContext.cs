namespace Capsule;

/// <summary>
/// The runtime context completely defines an environment where capsules can be run in.
/// </summary>
public record CapsuleRuntimeContext(ICapsuleHost Host, ICapsuleSynchronizerFactory SynchronizerFactory);
