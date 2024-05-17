// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Enums;
using RemoteMaster.Shared.Models;
using Serilog;

namespace RemoteMaster.Host.Core.Hubs;

[Authorize]
public class ControlHub : Hub<IControlClient>
{
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IScriptService _scriptService;
    private readonly IInputService _inputService;
    private readonly IPowerService _powerService;
    private readonly IHardwareService _hardwareService;
    private readonly IShutdownService _shutdownService;
    private readonly IScreenCapturerService _screenCapturerService;
    private readonly IHostConfigurationService _hostConfigurationService;
    private readonly IHostLifecycleService _hostLifecycleService;

    public ControlHub(IAppState appState, IViewerFactory viewerFactory, IScriptService scriptService, IInputService inputService, IPowerService powerService, IHardwareService hardwareService, IShutdownService shutdownService, IScreenCapturerService screenCapturerService, IHostConfigurationService hostConfigurationService, IHostLifecycleService hostLifecycleService)
    {
        _appState = appState;
        _viewerFactory = viewerFactory;
        _scriptService = scriptService;
        _inputService = inputService;
        _powerService = powerService;
        _hardwareService = hardwareService;
        _shutdownService = shutdownService;
        _screenCapturerService = screenCapturerService;
        _hostConfigurationService = hostConfigurationService;
        _hostLifecycleService = hostLifecycleService;

        _appState.ViewerAdded += OnViewerAdded;
        _appState.ViewerRemoved += OnViewerRemoved;
    }

    private async void OnViewerAdded(object? sender, IViewer viewer)
    {
        await NotifyAllViewers();
    }

    private async void OnViewerRemoved(object? sender, IViewer? viewer)
    {
        await NotifyAllViewers();
    }

    private async Task NotifyAllViewers()
    {
        var viewers = _appState.GetAllViewers().Select(v => new ViewerDto
        {
            ConnectionId = v.ConnectionId,
            UserName = v.UserName,
            ConnectedTime = v.ConnectedTime
        }).ToList();

        await Clients.All.ReceiveAllViewers(viewers);
    }

    public async Task ConnectAs(ConnectionRequest connectionRequest)
    {
        ArgumentNullException.ThrowIfNull(connectionRequest);

        switch (connectionRequest.Intention)
        {
            case Intention.ReceiveThumbnail:
                var thumbnail = _screenCapturerService.GetThumbnail(500, 300);

                if (thumbnail != null)
                {
                    await Clients.Caller.ReceiveThumbnail(thumbnail);
                }

                await Clients.Caller.ReceiveCloseConnection();

                break;

            case Intention.ManageDevice:
                var viewer = _viewerFactory.Create(Context.ConnectionId, connectionRequest.UserName);
                _appState.TryAddViewer(viewer);

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
        _appState.TryRemoveViewer(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseInput(MouseInputDto dto)
    {
        ExecuteActionForViewer(viewer => _inputService.SendMouseInput(dto, viewer.ScreenCapturer));
    }

    public void SendKeyboardInput(KeyboardInputDto dto)
    {
        _inputService.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(string displayName)
    {
        if (_appState.TryGetViewer(Context.ConnectionId, out var viewer))
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
        _inputService.InputEnabled = inputEnabled;
    }

    public void SendBlockUserInput(bool blockInput)
    {
        _inputService.BlockUserInput = blockInput;
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
        _shutdownService.ImmediateShutdown();
    }

    public void SendRebootComputer(PowerActionRequest powerActionRequest)
    {
        _powerService.Reboot(powerActionRequest);
    }

    public void SendShutdownComputer(PowerActionRequest powerActionRequest)
    {
        _powerService.Shutdown(powerActionRequest);
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
            Log.Error("Failed to find a viewer for connection ID {ConnectionId}", Context.ConnectionId);
        }
    }

    public void SendMonitorState(MonitorState state)
    {
        _hardwareService.SetMonitorState(state);
    }

    public void SendScript(ScriptExecutionRequest scriptExecutionRequest)
    {
        _scriptService.Execute(scriptExecutionRequest);
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
        var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync(false);

        hostConfiguration.Subject.OrganizationalUnit = newOrganizationalUnits;

        await _hostConfigurationService.SaveConfigurationAsync(hostConfiguration);
        await _hostLifecycleService.RenewCertificateAsync(hostConfiguration);
    }

    public async Task SendRenewCertificate()
    {
        var hostConfiguration = await _hostConfigurationService.LoadConfigurationAsync(false);

        await _hostLifecycleService.RenewCertificateAsync(hostConfiguration);
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