// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RemoteMaster.Server.Core.Abstractions;
using RemoteMaster.Shared.Dtos;

namespace RemoteMaster.Server.Core.Hubs;

public class ControlHub : Hub
{
    private readonly IAppState _appState;
    private readonly IViewerFactory _viewerFactory;
    private readonly IInputSender _inputSender;
    private readonly IPowerManager _powerManager;
    private readonly ILogger<ControlHub> _logger;

    private readonly IScreenCapturer _screenCapturer;

    public ControlHub(IAppState appState, IViewerFactory viewerFactory, IInputSender inputSender, IPowerManager powerManager, IScreenCapturer screenCapturer, ILogger<ControlHub> logger)
    {
        _appState = appState;
        _viewerFactory = viewerFactory;
        _inputSender = inputSender;
        _powerManager = powerManager;
        _screenCapturer = screenCapturer;
        _logger = logger;
    }

    public async Task ConnectAs(string intention)
    {
        switch (intention)
        {
            case "GetThumbnail":
                await Clients.Caller.SendAsync("ReceiveThumbnail", GetThumbnail());
                Context.Abort();
                break;

            case "StreamScreen":
                var viewer = _viewerFactory.Create(Context.ConnectionId);
                _appState.TryAddViewer(viewer);
                break;

            default:
                _logger.LogError("Unknown intention: {intention}", intention);
                break;
        }
    }

    private byte[] GetThumbnail()
    {
        const int maxWidth = 320;
        const int maxHeight = 240;

        return _screenCapturer.GetThumbnail(maxWidth, maxHeight);
    }

    public async override Task OnDisconnectedAsync(Exception? exception)
    {
        _appState.TryRemoveViewer(Context.ConnectionId, out var _);

        await base.OnDisconnectedAsync(exception);
    }

    public void SendMouseCoordinates(MouseMoveDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseCoordinates(dto, viewer));
    }

    public void SendMouseButton(MouseClickDto dto)
    {
        ExecuteActionForViewer(viewer => _inputSender.SendMouseButton(dto, viewer));
    }

    public void SendMouseWheel(MouseWheelDto dto)
    {
        _inputSender.SendMouseWheel(dto);
    }

    public void SendKeyboardInput(KeyboardKeyDto dto)
    {
        _inputSender.SendKeyboardInput(dto);
    }

    public void SendSelectedScreen(string displayName)
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

    public void SetInputEnabled(bool inputEnabled)
    {
        _inputSender.InputEnabled = inputEnabled;
    }

    public void SetQuality(int quality)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.Quality = quality);
    }

    public void SetTrackCursor(bool trackCursor)
    {
        ExecuteActionForViewer(viewer => viewer.ScreenCapturer.TrackCursor = trackCursor);
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method needs to be an instance method to be accessible by SignalR.")]
    public void KillServer()
    {
        Environment.Exit(0);
    }

    public void RebootComputer()
    {
        _powerManager.Reboot();
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
}
