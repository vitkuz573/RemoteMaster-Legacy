using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Server.Services;

public class AppStartup : IAppStartup
{
    private readonly ILogger<AppStartup> _logger;

    public AppStartup(ILogger<AppStartup> logger)
    {
        _logger = logger;
    }

    public async Task Initialize()
    {
        await StartScreenCastingAsync().ConfigureAwait(false);
    }

    private async Task StartScreenCastingAsync()
    {
        if (DesktopHelper.GetCurrentDesktop(out var currentDesktopName))
        {
            _logger.LogInformation("Setting initial desktop to {currentDesktopName}.", currentDesktopName);
        }
        else
        {
            _logger.LogWarning("Failed to get initial desktop name.");
        }

        if (!DesktopHelper.SwitchToInputDesktop())
        {
            _logger.LogWarning("Failed to set initial desktop.");
        }
    }
}