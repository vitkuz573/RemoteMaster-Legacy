// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class ControlHub : Hub<IControlClient>
{
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IScriptService _scriptService;
    private readonly IDomainService _domainService;
    private readonly IInputService _inputService;
    private readonly IPowerService _powerService;
    private readonly IHardwareService _hardwareService;
    private readonly IShutdownService _shutdownService;
    private readonly IUpdaterService _updaterService;
    private readonly IScreenCapturerService _screenCapturerService;
    private readonly IScreenRecorderService _screenRecorderService;

    public ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IDomainService domainService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IUpdaterService updaterService, IScreenCapturerService screenCapturerService, IScreenRecorderService screenRecorderService)
    {
        _appState = appState;
        _viewerFactory = viewerFactory;
        _scriptService = scriptService;
        _domainService = domainService;
        _inputService = inputService;
        _powerService = powerService;
        _hardwareService = hardwareService;
        _shutdownService = shutdownService;
        _updaterService = updaterService;
        _screenCapturerService = screenCapturerService;
        _screenRecorderService = screenRecorderService;
    }

    public async Task ConnectAs(Intention intention)
    {
        switch (intention)
        {
            case Intention.GetThumbnail:
                var thumbnail = _screenCapturerService.GetThumbnail(500, 300);

                if (thumbnail != null)
                {
                    await Clients.Caller.ReceiveThumbnail(thumbnail);
                }

                Context.Abort();
                break;

            case Intention.Connect:
                var viewer = _viewerFactory.Create(Context.ConnectionId);
                _appState.TryAddViewer(viewer);
                break;

            default:
                Log.Error("Unknown intention: {Intention}", intention);
                break;
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        _appState.TryRemoveViewer(Context.ConnectionId, out var _);

        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => _inputService.SendMouseCoordinates(dto, viewer));
    }

    public void SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => _inputService.SendMouseButton(dto, viewer));
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        _inputService.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputService.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(string displayName)
    {
        if (_appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            viewer?.SetSelectedScreen(displayName);
        }
        else
        {
            Log.Error("Failed to find a viewer for connection ID {connectionId}", Context.ConnectionId);
        }
    }

    public void SendToggleInput(bool inputEnabled)
    {
        _inputService.InputEnabled = inputEnabled;
    }

    public void SendImageQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.Quality = quality);
    }

    public void SendToggleCursorTracking(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    public void SendKillHost()
    {
        _shutdownService.ImmediateShutdown();
    }

    public void SendRebootComputer(string message, int timeout, bool forceAppsClosed)
    {
        _powerService.Reboot(message, (uint)timeout, forceAppsClosed);
    }

    public void SendShutdownComputer(string message, int timeout, bool forceAppsClosed)
    {
        _powerService.Shutdown(message, (uint)timeout, forceAppsClosed);
    }

    private void ExecuteActionForViewer(Action<IViewer> action)
    {
        if (_appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            if (viewer != null)
            {
                action(viewer);
            }
        }
        else
        {
            Log.Error("Failed to find a viewer for connection ID {connectionId}", Context.ConnectionId);
        }
    }

    public async Task SendStartScreenRecording(string outputPath)
    {
        await _screenRecorderService.StartRecordingAsync(outputPath);
    }

    public async Task SendStopScreenRecording()
    {
        await _screenRecorderService.StopRecordingAsync();
    }

    public void SendMonitorState(MonitorState state)
    {
        _hardwareService.SetMonitorState(state);
    }

    public void SendScript(string script, Shell shell)
    {
        _scriptService.Execute(shell, script);
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

    public void SendUpdateHost(string folderPath, string username, string password, bool isLocalFolder)
    {
        _updaterService.Download(folderPath, username, password, isLocalFolder);
        _updaterService.Execute();
    }

    public void SendJoinToDomain(string domain, string user, string password)
    {
        _domainService.JoinToDomain(domain, user, password);
    }

    public void SendUnjoinFromDomain(string user, string password)
    {
        _domainService.UnjoinFromDomain(user, password);
    }
}