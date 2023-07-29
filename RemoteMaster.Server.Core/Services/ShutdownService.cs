using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;

namespace RemoteMaster.Server.Core.Services;

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
