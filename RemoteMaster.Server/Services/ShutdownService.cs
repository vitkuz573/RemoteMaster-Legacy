using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ShutdownService : IShutdownService
{
    public void InitiateShutdown()
    {
        Environment.Exit(0);
    }
}
