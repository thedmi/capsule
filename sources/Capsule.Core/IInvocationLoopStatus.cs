namespace Capsule;

[Obsolete("Not used by Capsule anymore, use InvocationLoopStatus instead. Will be removed in v6.")]
public interface IInvocationLoopStatus
{
    bool Terminated { get; }
}
