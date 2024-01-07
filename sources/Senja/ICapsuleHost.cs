namespace Senja;

public interface ICapsuleHost
{
    Task RegisterAsync(ICapsuleEventLoop capsuleEventLoop);
}
