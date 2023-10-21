// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Helpers.AdvFirewall;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class ControlHub : Hub<IControlClient>
{
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IInputService _inputService;
    private readonly IPowerService _powerManager;
    private readonly IHardwareService _hardwareService;
    private readonly IShutdownService _shutdownService;
    private readonly IUpdaterService _updaterService;
    private readonly IScreenCapturerService _screenCapturerService;
    private readonly IScreenRecorderService _screenRecorderService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(IAppState appState, IViewerFactory viewerFactory, IInputService inputSender, IPowerService powerManager, IHardwareService hardwareService, IShutdownService shutdownService, IUpdaterService updaterService, IScreenCapturerService screenCapturer, IScreenRecorderService screenRecorderService, ILogger<ControlHub> logger)
    {
        _appState = appState;
        _viewerFactory = viewerFactory;
        _inputService = inputSender;
        _powerManager = powerManager;
        _hardwareService = hardwareService;
        _shutdownService = shutdownService;
        _updaterService = updaterService;
        _screenCapturerService = screenCapturer;
        _screenRecorderService = screenRecorderService;
        _logger = logger;
    }

    public async Task ConnectAs(Intention intention)
    {
        switch (intention)
        {
            case Intention.GetThumbnail:
                await Clients.Caller.ReceiveThumbnail(GetThumbnail());
                Context.Abort();
                break;

            case Intention.Connect:
                var viewer = _viewerFactory.Create(Context.ConnectionId);
                _appState.TryAddViewer(viewer);
                break;

            default:
                _logger.LogError("Unknown intention: {Intention}", intention);
                break;
        }
    }

    private byte[] GetThumbnail()
    {
        const int maxWidth = 500;
        const int maxHeight = 300;

        return _screenCapturerService.GetThumbnail(maxWidth, maxHeight);
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        _appState.TryRemoveViewer(Context.ConnectionId, out var _);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => _inputService.SendMouseCoordinates(dto, viewer));
    }

    public async Task SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => _inputService.SendMouseButton(dto, viewer));
    }

    public async Task SendMouseWheel(MouseWheelDto dto)
    {
        _inputService.SendMouseWheel(dto);
    }

    public async Task SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputService.SendKeyboardInput(dto);
    }

    public async Task SendSelectedScreen(string displayName)
    {
        if (_appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            viewer.SetSelectedScreen(displayName);
        }
        else
        {
            _logger.LogError("Failed to find a viewer for connection ID {connectionId}", Context.ConnectionId);
        }
    }

    public async Task SetInputEnabled(bool inputEnabled)
    {
        _inputService.InputEnabled = inputEnabled;
    }

    public async Task SetQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.Quality = quality);
    }

    public async Task SetTrackCursor(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    public async Task KillHost()
    {
        _shutdownService.ImmediateShutdown();
    }

    public async Task RebootComputer(string message, int timeout, bool forceAppsClosed)
    {
        _powerManager.Reboot(message, (uint)timeout, forceAppsClosed);
    }

    public async Task ShutdownComputer(string message, int timeout, bool forceAppsClosed)
    {
        _powerManager.Shutdown(message, (uint)timeout, forceAppsClosed);
    }

    private void ExecuteActionForViewer(Action<IViewer> action)
    {
        if (_appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            action(viewer);
        }
        else
        {
            _logger.LogError("Failed to find a viewer for connection ID {connectionId}", Context.ConnectionId);
        }
    }

    public async Task StartScreenRecording(string outputPath)
    {
        await _screenRecorderService.StartRecordingAsync(outputPath);
    }

    public async Task StopScreenRecording()
    {
        await _screenRecorderService.StopRecordingAsync();
    }

    public async Task SetMonitorState(MonitorState state)
    {
        _hardwareService.SetMonitorState(state);
    }

    [SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    public async Task ExecuteScript(string scriptContent, string shellType)
    {
        // _logger.LogInformation("Executing script with shell type: {ShellType}", shellType);
        // 
        // var publicDirectory = @"C:\Users\Public";
        // var fileName = $"{Guid.NewGuid()}";
        // 
        // if (shellType == "CMD")
        // {
        //     fileName += ".bat";
        // }
        // else if (shellType == "PowerShell")
        // {
        //     fileName += ".ps1";
        // }
        // else
        // {
        //     _logger.LogError("Unsupported shell type encountered: {ShellType}", shellType);
        //     
        //     throw new InvalidOperationException($"Unsupported shell type: {shellType}");
        // }
        // 
        // var tempFilePath = Path.Combine(publicDirectory, fileName);
        // 
        // _logger.LogInformation("Temporary file path: {TempFilePath}", tempFilePath);
        // File.WriteAllText(tempFilePath, scriptContent);
        // 
        // try
        // {
        //     if (!File.Exists(tempFilePath))
        //     {
        //         _logger.LogError("Temp file was not created: {TempFilePath}", tempFilePath);
        //         
        //         return;
        //     }
        // 
        //     var applicationToRun = shellType switch
        //     {
        //         "CMD" => $"cmd.exe /c \"{tempFilePath}\"",
        //         "PowerShell" => $"powershell.exe -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
        //         _ => "",
        //     };
        // 
        //     var options = new ProcessStartOptions(applicationToRun, -1)
        //     {
        //         ForceConsoleSession = true,
        //         DesktopName = "default",
        //         HiddenWindow = true,
        //         UseCurrentUserToken = false
        //     };
        // 
        //     using var nativeProcess = NativeProcess.Start(options);
        //     nativeProcess.OutputReceived += (output) => {
        //         _logger.LogInformation(output);
        // 
        //         _ = Clients.Caller.ReceiveScriptResult(output);
        //     };
        //     await nativeProcess.StartListeningToOutputAsync();
        // }
        // finally
        // {
        //     if (File.Exists(tempFilePath))
        //     {
        //         File.Delete(tempFilePath);
        //     }
        // }
    }

    [SuppressMessage("Performance", "CA1822:Пометьте члены как статические", Justification = "<Ожидание>")]
    public async Task SetPSExecRules(bool enable)
    {
        if (enable)
        {
            FirewallManager.DeleteRule("PSExec", RuleDirection.In);
            FirewallManager.SetRuleGroup("Удаленное управление службой", RuleGroupStatus.Disabled);

            FirewallManager.EnableWinRM();

            var rule = new FirewallRule("PSExec")
            {
                Direction = RuleDirection.In,
                Action = RuleAction.Allow,
                Protocol = RuleProtocol.TCP,
                LocalPort = "RPC",
                Program = @"%WinDir%\system32\services.exe",
                Service = "any"
            };

            rule.Profiles.Add(RuleProfile.Domain);
            rule.Profiles.Add(RuleProfile.Private);
            rule.Apply();

            FirewallManager.SetRuleGroup("Удаленное управление службой", RuleGroupStatus.Enabled);
        }
        else
        {
            FirewallManager.DeleteRule("PSExec", RuleDirection.In);
            FirewallManager.SetRuleGroup("Удаленное управление службой", RuleGroupStatus.Disabled);
        }
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendCommandToService(string command)
    {
        await Clients.Group("serviceGroup").ReceiveCommand(command);
    }

    public async Task SendUpdateHost(string sharedFolder, string username, string password)
    {
        _updaterService.Download(sharedFolder, username, password);
        _updaterService.Execute();
    }
}