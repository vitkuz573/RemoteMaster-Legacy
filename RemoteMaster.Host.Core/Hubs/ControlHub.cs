// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using Serilog;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturerService screenCapturerService, IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService) : Hub<IControlClient>
{
    public async Task ConnectAs(ConnectionRequest connectionRequest)
    {
        ArgumentNullException.ThrowIfNull(connectionRequest);

        switch (connectionRequest.Intention)
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
                var viewer = viewerFactory.Create(Context.ConnectionId, connectionRequest.UserName);
                appState.TryAddViewer(viewer);

                var assembly = Assembly.GetEntryAssembly();
                var version = assembly?.GetName().Version ?? new Version();

                await Clients.Caller.ReceiveHostVersion(version);

                var transportType = Context.Features.Get<IHttpTransportFeature>().TransportType;
                await Clients.Caller.ReceiveTransportType(transportType.ToString());
                break;
            default:
                Log.Error("Unknown intention: {Intention}", connectionRequest.Intention);
                break;
        }
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        if (appState.TryGetViewer(Context.ConnectionId, out var viewer))
        {
            viewer?.StopStreaming();
            appState.TryRemoveViewer(Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    [Authorize(Roles = "Administrator")]
    public void SendMouseInput(MouseInputDto dto)
    {
        ExecuteActionForViewer(viewer => inputService.SendMouseInput(dto, viewer.ScreenCapturer));
    }

    [Authorize(Roles = "Administrator")]
    public void SendKeyboardInput(KeyboardInputDto dto)
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

    public async Task SendToggleInput(bool inputEnabled)
    {
        if (inputEnabled)
        {
            if (Context.User.IsInRole("Administrator"))
            {
                inputService.InputEnabled = inputEnabled;
            }
            else
            {
                await Clients.Caller.ReceiveError("Access Denied: Only administrators can enable input.");
            }
        }
        else
        {
            inputService.InputEnabled = inputEnabled;
        }
    }

    [Authorize(Roles = "Administrator")]
    public void SendBlockUserInput(bool blockInput)
    {
        inputService.BlockUserInput = blockInput;
    }

    public void SendImageQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.ImageQuality = quality);
    }

    public void SendToggleCursorTracking(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    [Authorize(Roles = "Administrator")]
    public void SendKillHost()
    {
        shutdownService.ImmediateShutdown();
    }

    [Authorize(Roles = "Administrator")]
    public void SendRebootComputer(PowerActionRequest powerActionRequest)
    {
        powerService.Reboot(powerActionRequest);
    }

    [Authorize(Roles = "Administrator")]
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

    [Authorize(Roles = "Administrator")]
    public void SendMonitorState(MonitorState state)
    {
        hardwareService.SetMonitorState(state);
    }

    [Authorize(Roles = "Administrator")]
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

    [Authorize(Roles = "Administrator")]
    public async Task ChangeOrganizationalUnit(string[] newOrganizationalUnits)
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        hostConfiguration.Subject.OrganizationalUnit = newOrganizationalUnits;

        await hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
        await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
    }

    [Authorize(Roles = "Administrator")]
    public async Task SendRenewCertificate()
    {
        var hostConfiguration = await hostConfigurationService.LoadConfigurationAsync(false);

        await hostLifecycleService.RenewCertificateAsync(hostConfiguration);
    }

#pragma warning disable CA1822
    public async Task<string> GetCertificateSerialNumber()
#pragma warning restore CA1822
    {
        X509Certificate2? certificate = null;

        using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
        {
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(X509FindType.FindBySubjectName, Dns.GetHostName(), false);

            foreach (var cert in certificates)
            {
                if (cert.HasPrivateKey)
                {
                    certificate = cert;
                    break;
                }
            }
        }

        return certificate.GetSerialNumberString();
    }
}