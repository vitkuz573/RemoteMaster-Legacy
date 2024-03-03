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
public class ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturerService screenCapturerService, IHostConfigurationService hostConfigurationService, IPsExecService psExecService, IHostLifecycleService hostLifecycleService) : Hub<IControlClient>
{
    public async Task ConnectAs(Intention intention)
    {
        switch (intention)
        {
            case Intention.ReceiveThumbnail:
                var thumbnail = screenCapturerService.GetThumbnail(500, 300);

                if (thumbnail != null)
                {
                    await Clients.Caller.ReceiveThumbnail(thumbnail);
                }

                await Clients.Caller.ReceiveCloseConnection();

                break;

            case Intention.ManageDevice:
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
        appState.TryRemoveViewer(Context.ConnectionId);

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
            viewer?.ScreenCapturer.SetSelectedScreen(displayName);
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
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.ImageQuality = quality);
    }

    public void SendToggleCursorTracking(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    public void SendKillHost()
    {
        shutdownService.ImmediateShutdown();
    }

    public void SendRebootComputer(PowerActionRequest powerActionRequest)
    {
        powerService.Reboot(powerActionRequest);
    }

    public void SendShutdownComputer(PowerActionRequest powerActionRequest)
    {
        powerService.Shutdown(powerActionRequest);
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

    public void SendMonitorState(MonitorState state)
    {
        hardwareService.SetMonitorState(state);
    }

    public void SendScript(ScriptExecutionRequest scriptExecutionRequest)
    {
        scriptService.Execute(scriptExecutionRequest);
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

    public async Task ChangeOrganizationalUnit(string[] newOrganizationalUnits)
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        hostConfiguration.Subject.OrganizationalUnit = newOrganizationalUnits;

        await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);

        await hostLifecycleService.UnregisterAsync(hostConfiguration);
        await hostLifecycleService.RegisterAsync(hostConfiguration);
    }

    public async Task SetPsExecRules(bool enable)
    {
        await psExecService.DisableAsync();

        if (enable)
        {
            await psExecService.EnableAsync();
        }
    }
}