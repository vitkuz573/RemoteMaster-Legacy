using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Shared.Native.Windows;
using System.Diagnostics;

namespace RemoteMaster.Agent.Hubs;

public class MainHub : Hub
{
    private static readonly string SERVER_NAME = "RemoteMaster.Server";
    private static readonly string SERVER_PATH = @"C:\sc\RemoteMaster.Server\RemoteMaster.Server.exe";

    private readonly ILogger<MainHub> _logger;

    public MainHub(ILogger<MainHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        if (!IsServerRunning())
        {
            try
            {
                ProcessHelper.OpenInteractiveProcess(SERVER_PATH, -1, true, "default", true, out _);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting RemoteMaster Server");
            }
        }

        await base.OnConnectedAsync();
    }

    private static bool IsServerRunning() => Process.GetProcessesByName(SERVER_NAME).Length > 0;
}
