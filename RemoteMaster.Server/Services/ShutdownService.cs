using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class ShutdownService : IShutdownService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<ShutdownService> _logger;

    public ShutdownService(IHostApplicationLifetime appLifetime, ILogger<ShutdownService> logger)
    {
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public void InitiateShutdown()
    {
        _logger.LogInformation("Initiating shutdown...");
        _appLifetime.StopApplication();
    }
}
