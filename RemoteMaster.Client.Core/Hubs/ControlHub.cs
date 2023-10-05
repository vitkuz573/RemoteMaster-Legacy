// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Client.Core.Helpers.AdvFirewall;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using RemoteMaster.Shared.Native.Windows;

namespace RemoteMaster.Client.Core.Hubs;

public class ControlHub : Hub<IControlClient>, IControlHub
{
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IInputService _inputSender;
    private readonly IPowerService _powerManager;
    private readonly IHardwareService _hardwareService;
    private readonly IShutdownService _shutdownService;
    private readonly IScreenCapturerService _screenCapturer;
    private readonly IScreenRecorderService _screenRecorderService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(IAppState appState, IViewerFactory viewerFactory, IInputService inputSender, IPowerService powerManager, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturerService screenCapturer, IScreenRecorderService screenRecorderService, ILogger<ControlHub> logger)
    {
        _appState = appState;
        _viewerFactory = viewerFactory;
        _inputSender = inputSender;
        _powerManager = powerManager;
        _hardwareService = hardwareService;
        _shutdownService = shutdownService;
        _screenCapturer = screenCapturer;
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

        return _screenCapturer.GetThumbnail(maxWidth, maxHeight);
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        _appState.TryRemoveViewer(Context.ConnectionId, out var _);

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseCoordinates(dto, viewer));
    }

    public async Task SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseButton(dto, viewer));
    }

    public async Task SendMouseWheel(MouseWheelDto dto)
    {
        _inputSender.SendMouseWheel(dto);
    }

    public async Task SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputSender.SendKeyboardInput(dto);
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
        _inputSender.InputEnabled = inputEnabled;
    }

    public async Task SetQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.Quality = quality);
    }

    public async Task SetTrackCursor(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    public async Task KillClient()
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

    public async Task ExecuteScript(string scriptContent, string shellType)
    {
        _logger.LogInformation("Executing script with shell type: {ShellType}", shellType);

        var publicDirectory = @"C:\Users\Public";
        var fileName = $"{Guid.NewGuid()}";

        if (shellType == "CMD")
        {
            fileName += ".bat";
        }
        else if (shellType == "PowerShell")
        {
            fileName += ".ps1";
        }
        else
        {
            _logger.LogError("Unsupported shell type encountered: {ShellType}", shellType);
            throw new InvalidOperationException($"Unsupported shell type: {shellType}");
        }

        var tempFilePath = Path.Combine(publicDirectory, fileName);

        _logger.LogInformation("Temporary file path: {TempFilePath}", tempFilePath);
        File.WriteAllText(tempFilePath, scriptContent);

        try
        {
            if (!File.Exists(tempFilePath))
            {
                _logger.LogError("Temp file was not created: {TempFilePath}", tempFilePath);
                return;
            }

            var applicationToRun = shellType switch
            {
                "CMD" => $"cmd.exe /c \"{tempFilePath}\"",
                "PowerShell" => $"powershell.exe -ExecutionPolicy Bypass -File \"{tempFilePath}\"",
                _ => "",
            };

            if (!ProcessHelper.OpenInteractiveProcess(applicationToRun, -1, true, "default", true, true, out var procInfo))
            {
                _logger.LogError("Failed to start interactive process for: {ApplicationToRun}", applicationToRun);
            }
            else
            {
                _logger.LogInformation("Process started with ID: {ProcessID}, Thread ID: {ThreadID}", procInfo.dwProcessId, procInfo.dwThreadId);

                var process = Process.GetProcessById((int)procInfo.dwProcessId);
                process.WaitForExit();

                await Clients.Caller.ReceiveScriptResult($"Script executed");
            }
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

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
}