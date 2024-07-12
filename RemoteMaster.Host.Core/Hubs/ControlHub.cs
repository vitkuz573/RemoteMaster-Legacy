// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize(Policy = "LocalhostOrAuthenticatedPolicy")]
public class ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturerService screenCapturerService, IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService, ICertificateStoreService certificateStoreService, IWorkStationSecurityService workStationSecurityService, IScreenCastingService screenCastingService) : Hub<IControlClient>
{
    public async override Task OnConnectedAsync()
    {
        var user = Context.User;

        if (user != null)
        {
            var userName = user.FindFirst(ClaimTypes.Name)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            var httpContext = Context.GetHttpContext();

            if (httpContext != null)
            {
                var query = httpContext.Request.Query;
                var isThumbnailRequest = query.ContainsKey("thumbnail") && query["thumbnail"] == "true";
                var isScreenCastRequest = query.ContainsKey("screencast") && query["screencast"] == "true";

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

                if (isThumbnailRequest)
                {
                    await SendThumbnailAsync();
                    await Clients.Caller.ReceiveCloseConnection();

                    return;
                }

                if (isScreenCastRequest && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(role))
                {
                    var viewer = viewerFactory.Create(Context.ConnectionId, "Users", userName, role, ipAddress);
                    appState.TryAddViewer(viewer);

                    screenCastingService.StartStreaming(viewer);

                    var assembly = Assembly.GetEntryAssembly();
                    var version = assembly?.GetName().Version ?? new Version();

                    await Clients.Caller.ReceiveHostVersion(version);

                    var transportFeature = Context.Features.Get<IHttpTransportFeature>();

                    if (transportFeature != null)
                    {
                        await Clients.Caller.ReceiveTransportType(transportFeature.TransportType.ToString());
                    }
                    else
                    {
                        await Clients.Caller.ReceiveTransportType("Unknown");
                    }
                }
            }
        }

        await base.OnConnectedAsync();
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        if (appState.TryGetViewer(Context.ConnectionId, out var viewer) && viewer != null)
        {
            screenCastingService.StopStreaming(viewer);
            appState.TryRemoveViewer(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendThumbnailAsync()
    {
        var thumbnail = screenCapturerService.GetThumbnail(500, 300);

        if (thumbnail != null)
        {
            await Clients.Caller.ReceiveThumbnail(thumbnail);
        }
    }

    [Authorize(Policy = "MouseInputPolicy")]
    public void SendMouseInput(MouseInputDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.SendMouseInput(dto, viewer.ScreenCapturer));
    }

    [Authorize(Policy = "KeyboardInputPolicy")]
    public void SendKeyboardInput(KeyboardInputDto dto)
    {
        inputService.SendKeyboardInput(dto);
    }

    [Authorize(Policy = "SwitchScreenPolicy")]
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

    [Authorize(Policy = "ToggleInputPolicy")]
    public void SendToggleInput(bool inputEnabled)
    {
        inputService.InputEnabled = inputEnabled;
    }

    [Authorize(Policy = "ToggleUserInputPolicy")]
    public void SendBlockUserInput(bool blockInput)
    {
        inputService.BlockUserInput = blockInput;
    }

    [Authorize(Policy = "ChangeImageQualityPolicy")]
    public void SendImageQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.ImageQuality = quality);
    }

    [Authorize(Policy = "ToggleCursorTrackingPolicy")]
    public void SendToggleCursorTracking(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    [Authorize(Policy = "TerminateHostPolicy")]
    public void SendKillHost()
    {
        shutdownService.ImmediateShutdown();
    }

    [Authorize(Policy = "RebootComputerPolicy")]
    public void SendRebootComputer(PowerActionRequest powerActionRequest)
    {
        powerService.Reboot(powerActionRequest);
    }

    [Authorize(Policy = "ShutdownComputerPolicy")]
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

    [Authorize(Policy = "ChangeMonitorStatePolicy")]
    public void SendMonitorState(MonitorState state)
    {
        hardwareService.SetMonitorState(state);
    }

    [Authorize(Policy = "ExecuteScriptPolicy")]
    public void SendScript(ScriptExecutionRequest scriptExecutionRequest)
    {
        scriptService.Execute(scriptExecutionRequest);
    }

    [Authorize(Policy = "LockWorkStationPolicy")]
    public void SendLockWorkStation()
    {
        workStationSecurityService.LockWorkStationDisplay();
    }

    [Authorize(Policy = "LogOffUserPolicy")]
    public void SendLogOffUser(bool force)
    {
        workStationSecurityService.LogOffUser(force);
    }

    public async Task JoinGroup(string groupName)
    {
        var viewer = viewerFactory.Create(Context.ConnectionId, groupName, "RCHost", "Windows Service", "127.0.0.1");
        appState.TryAddViewer(viewer);

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task SendCommandToService(string command)
    {
        Log.Information("Received command: {Command}", command);

        await Clients.Group("Services").ReceiveCommand(command);
    }

    [Authorize(Policy = "MovePolicy")]
    public async Task Move(HostMoveRequest hostMoveRequest)
    {
        ArgumentNullException.ThrowIfNull(hostMoveRequest);

        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        hostConfiguration.Subject.Organization = hostMoveRequest.NewOrganization;
        hostConfiguration.Subject.OrganizationalUnit = hostMoveRequest.NewOrganizationalUnit;

        await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
        await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
    }

    [Authorize(Policy = "RenewCertificatePolicy")]
    public async Task SendRenewCertificate()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
    }

    public string? GetCertificateSerialNumber()
    {
        var certificates = certificateStoreService.GetCertificates(StoreName.My, StoreLocation.LocalMachine, X509FindType.FindBySubjectName, Dns.GetHostName());
        var certificate = certificates.FirstOrDefault(c => c.HasPrivateKey);

        return certificate?.GetSerialNumberString();
    }
}