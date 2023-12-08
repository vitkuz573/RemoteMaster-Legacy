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
public class ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IDomainService domainService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IUpdaterService updaterService, IScreenCapturerService screenCapturerService, IScreenRecorderService screenRecorderService) : Hub<IControlClient>
{
    public async Task ConnectAs(Intention intention)
    {
        switch (intention)
        {
            case Intention.GetThumbnail:
                var thumbnail = screenCapturerService.GetThumbnail(500, 300);

                if (thumbnail != null)
                {
                    await Clients.Caller.ReceiveThumbnail(thumbnail);
                }

                Context.Abort();
                break;

            case Intention.Connect:
                var viewer = viewerFactory.Create(Context.ConnectionId);
                appState.TryAddViewer(viewer);
                break;

            default:
                Log.Error("Unknown intention: {Intention}", intention);
                break;
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        appState.TryRemoveViewer(Context.ConnectionId, out var _);

        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.SendMouseCoordinates(dto, viewer));
    }

    public void SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.SendMouseButton(dto, viewer));
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        inputService.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        inputService.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(string displayName)
    {
        if (appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            viewer?.SetSelectedScreen(displayName);
        }
        else
        {
            Log.Error("Failed to find a viewer for connection ID {ConnectionId}", Context.ConnectionId);
        }
    }

    public void SendToggleInput(bool inputEnabled)
    {
        inputService.InputEnabled = inputEnabled;
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
        shutdownService.ImmediateShutdown();
    }

    public void SendRebootComputer(string message, int timeout, bool forceAppsClosed)
    {
        powerService.Reboot(message, (uint)timeout, forceAppsClosed);
    }

    public void SendShutdownComputer(string message, int timeout, bool forceAppsClosed)
    {
        powerService.Shutdown(message, (uint)timeout, forceAppsClosed);
    }

    private void ExecuteActionForViewer(Action<IViewer> action)
    {
        if (appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            if (viewer != null)
            {
                action(viewer);
            }
        }
        else
        {
            Log.Error("Failed to find a viewer for connection ID {ConnectionId}", Context.ConnectionId);
        }
    }

    public async Task SendStartScreenRecording(string outputPath)
    {
        await screenRecorderService.StartRecordingAsync(outputPath);
    }

    public async Task SendStopScreenRecording()
    {
        await screenRecorderService.StopRecordingAsync();
    }

    public void SendMonitorState(MonitorState state)
    {
        hardwareService.SetMonitorState(state);
    }

    public void SendScript(string script, Shell shell)
    {
        scriptService.Execute(shell, script);
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
        updaterService.Execute(folderPath, username, password, isLocalFolder);
    }

    public void SendJoinToDomain(string domain, string user, string password)
    {
        domainService.JoinToDomain(domain, user, password);
    }

    public void SendUnjoinFromDomain(string user, string password)
    {
        domainService.UnjoinFromDomain(user, password);
    }
}