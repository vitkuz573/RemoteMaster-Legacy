// DummyScreenCapturingService.cs
// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Drawing;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

public abstract class ScreenCapturingService(IAppState appState, IOverlayManagerService overlayManagerService, IScreenProvider screenProvider, ILogger<ScreenCapturingService> logger): IScreenCapturingService
{
    private readonly Lock _screenBoundsLock = new();

    private readonly ConcurrentDictionary<string, EventHandler> _isCursorVisibleHandlers = new();

    private bool HasMultipleScreens => screenProvider.GetAllScreens().Count() > 1;

    public IScreen? FindScreenByName(string displayName)
    {
        var allScreens = screenProvider.GetAllScreens().ToList();

        if (HasMultipleScreens)
        {
            allScreens.Add(screenProvider.GetVirtualScreen());
        }

        return allScreens.FirstOrDefault(screen => screen.DeviceName == displayName);
    }

    protected abstract byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec);

    private byte[]? GetFrame(string connectionId)
    {
        try
        {
            if (!appState.TryGetViewer(connectionId, out var viewer) || viewer?.CapturingContext.SelectedScreen == null)
            {
                logger.LogWarning("Viewer not found or SelectedScreen is null for ConnectionId: {ConnectionId}", connectionId);

                return null;
            }

            var capturingContext = viewer.CapturingContext;

            return capturingContext.SelectedScreen.DeviceName == screenProvider.GetVirtualScreen().DeviceName
                ? GetVirtualScreenFrame(connectionId, capturingContext.ImageQuality, capturingContext.SelectedCodec)
                : GetSingleScreenFrame(connectionId, capturingContext.SelectedScreen.Bounds, capturingContext.ImageQuality, capturingContext.SelectedCodec);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in GetFrame for ConnectionId: {ConnectionId}", connectionId);

            return null;
        }
    }

    private byte[] GetVirtualScreenFrame(string connectionId, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, screenProvider.GetVirtualScreen().Bounds, imageQuality, codec);
    }

    private byte[] GetSingleScreenFrame(string connectionId, Rectangle bounds, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, bounds, imageQuality, codec);
    }

    public virtual IEnumerable<Display> GetDisplays()
    {
        var screens = screenProvider.GetAllScreens().Select(screen => new Display { Name = screen.DeviceName, IsPrimary = screen.Primary, Resolution = screen.Bounds.Size }).ToList();

        if (screenProvider.GetAllScreens().Count() <= 1)
        {
            return screens;
        }

        var virtualScreen = screenProvider.GetVirtualScreen();

        screens.Add(new Display
        {
            Name = virtualScreen.DeviceName,
            IsPrimary = false,
            Resolution = virtualScreen.Bounds.Size
        });

        return screens;
    }

    public virtual void SetSelectedScreen(string connectionId, IScreen display)
    {
        ArgumentNullException.ThrowIfNull(display);

        if (!appState.TryGetViewer(connectionId, out var viewer) || viewer == null)
        {
            logger.LogError("Viewer not found for connection: {ConnectionId}", connectionId);
            
            return;
        }

        var capturingContext = viewer.CapturingContext;
        
        if (capturingContext.SelectedScreen != null && capturingContext.SelectedScreen.Equals(display))
        {
            logger.LogInformation("Selected screen is already set for connection: {ConnectionId}", connectionId);
            
            return;
        }

        capturingContext.SelectedScreen = display;

        logger.LogInformation("Selected screen set to {ScreenName} for connection: {ConnectionId}", display.DeviceName, connectionId);
    }

    public virtual byte[]? GetNextFrame(string connectionId)
    {
        using (_screenBoundsLock.EnterScope())
        {
            try
            {
                appState.TryGetViewer(connectionId, out var viewer);

                var capturingContext = viewer?.CapturingContext;

                if (capturingContext?.SelectedScreen != null)
                {
                    return GetFrame(connectionId);
                }

                logger.LogWarning("Selected screen is null for connection ID {ConnectionId}", connectionId);

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while getting next frame.");

                return null;
            }
        }
    }

    public virtual byte[]? GetThumbnail(string connectionId)
    {
        var targetScreen = HasMultipleScreens ? screenProvider.GetVirtualScreen() : screenProvider.GetPrimaryScreen();

        if (targetScreen == null)
        {
            logger.LogError("No screens available for thumbnail generation.");

            return null;
        }

        SetSelectedScreen(connectionId, targetScreen);

        return GetNextFrame(connectionId);
    }

    private void OnViewerAdded(object? sender, IViewer? viewer)
    {
        if (viewer != null)
        {
            SubscribeToViewer(viewer);
        }
    }

    private void OnViewerRemoved(object? sender, IViewer? viewer)
    {
        if (viewer != null)
        {
            UnsubscribeFromViewer(viewer);
        }
    }

    private void SubscribeToViewer(IViewer viewer)
    {
        var context = viewer.CapturingContext;

        EventHandler handler = (_, _) => HandleIsCursorVisibleChanged(viewer);
        context.OnIsCursorVisibleChanged += handler;

        _isCursorVisibleHandlers.TryAdd(viewer.ConnectionId, handler);

        if (context.IsCursorVisible)
        {
            // overlayManagerService.ActivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
    }

    private void UnsubscribeFromViewer(IViewer viewer)
    {
        var context = viewer.CapturingContext;

        if (_isCursorVisibleHandlers.TryRemove(viewer.ConnectionId, out var handler))
        {
            context.OnIsCursorVisibleChanged -= handler;
        }

        // overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
    }

    private void HandleIsCursorVisibleChanged(IViewer? viewer)
    {
        if (viewer == null)
        {
            logger.LogWarning("Viewer is null when handling IsCursorVisibleChanged event.");

            return;
        }

        if (viewer.CapturingContext.IsCursorVisible)
        {
            // overlayManagerService.ActivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
        else
        {
            // overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
    }

    public virtual void Dispose()
    {
        appState.ViewerAdded -= OnViewerAdded;
        appState.ViewerRemoved -= OnViewerRemoved;

        foreach (var viewer in appState.GetAllViewers())
        {
            UnsubscribeFromViewer(viewer);
        }
    }
}
