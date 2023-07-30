using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Agent.Hubs;

[SupportedOSPlatform("windows6.0.6000")]
public class MainHub : Hub
{
    private readonly string _serverPath;
    private readonly string _serverName;

    private readonly ILogger<MainHub> _logger;

    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
#if DEBUG
        _serverPath = @"C:\sc\RemoteMaster.Server\RemoteMaster.Server.exe";
#else
        _serverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"RemoteMaster\Server\RemoteMaster.Server.exe");
#endif
        _serverName = Path.GetFileNameWithoutExtension(_serverPath);
    }

    public async override Task OnConnectedAsync()
    {
        if (!IsServerRunning())
        {
            try
            {
                ProcessHelper.OpenInteractiveProcess(_serverPath, -1, true, "default", true, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RemoteMaster Server");
            }
        }

        await base.OnConnectedAsync();
    }

    private bool IsServerRunning() => Process.GetProcessesByName(_serverName).Length > 0;
}
