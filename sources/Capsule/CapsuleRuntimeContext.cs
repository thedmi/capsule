namespace Capsule;

public record CapsuleRuntimeContext(ICapsuleHost Host, ICapsuleInvocationLoopFactory InvocationLoopFactory);
