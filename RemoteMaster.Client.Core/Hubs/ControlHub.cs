// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Client.Core.Abstractions;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Dtos;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Core.Hubs;

public class ControlHub : Hub<IControlClient>, IControlHub
{
    private readonly IAgentUpdater _agentUpdater;
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IInputService _inputSender;
    private readonly IPowerService _powerManager;
    private readonly IShutdownService _shutdownService;
    private readonly IScreenCapturerService _screenCapturer;
    private readonly IScreenRecorderService _screenRecorderService;
    private readonly ILogger<ControlHub> _logger;

    public ControlHub(IAgentUpdater agentUpdater, IAppState appState, IViewerFactory viewerFactory, IInputService inputSender, IPowerService powerManager, IShutdownService shutdownService, IScreenCapturerService screenCapturer, IScreenRecorderService screenRecorderService, ILogger<ControlHub> logger)
    {
        _agentUpdater = agentUpdater;
        _appState = appState;
        _viewerFactory = viewerFactory;
        _inputSender = inputSender;
        _powerManager = powerManager;
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

            case Intention.Control:
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

    public async Task SendAgentUpdate()
    {
        _agentUpdater.Update();
    }

    public async Task StartScreenRecording(string outputPath)
    {
        await _screenRecorderService.StartRecordingAsync(outputPath);
    }

    public async Task StopScreenRecording()
    {
        await _screenRecorderService.StopRecordingAsync();
    }
}