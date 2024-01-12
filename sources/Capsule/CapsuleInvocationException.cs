namespace Capsule;

public class CapsuleInvocationException : Exception
{
    public CapsuleInvocationException(string? message) : base(message)
    {
    }

    public CapsuleInvocationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
