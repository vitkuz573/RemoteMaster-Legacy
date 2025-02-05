// DummyScreenCapturingService.cs
// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

public abstract class ScreenCapturingService(IAppState appState, IOverlayManagerService overlayManagerService, IScreenProvider screenProvider, ILogger<ScreenCapturingService> logger): IScreenCapturingService
{
    protected abstract byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec);

    public virtual byte[]? GetNextFrame(string connectionId)
    {
        try
        {
            var screen = screenProvider.GetPrimaryScreen();
            
            if (screen != null)
            {
                return CaptureScreen(connectionId, screen.Bounds, imageQuality: 75, codec: "image/png");
            }

            logger.LogError("No primary screen available.");

            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in GetNextFrame for connection: {ConnectionId}", connectionId);

            return null;
        }
    }

    /// <summary>
    /// Returns a list of available displays.
    /// </summary>
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

    /// <summary>
    /// Finds a screen by its device name.
    /// </summary>
    public virtual IScreen? FindScreenByName(string displayName)
    {
        var allScreens = new List<IScreen>(screenProvider.GetAllScreens());

        if (screenProvider.GetAllScreens().Count() > 1)
        {
            allScreens.Add(screenProvider.GetVirtualScreen());
        }

        return allScreens.Find(screen => screen.DeviceName.Equals(displayName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Sets the selected screen for a given connection.
    /// Updates the capturing context for the viewer.
    /// </summary>
    public virtual void SetSelectedScreen(string connectionId, IScreen display)
    {
        if (display == null)
        {
            throw new ArgumentNullException(nameof(display));
        }

        if (!appState.TryGetViewer(connectionId, out var viewer) || viewer == null)
        {
            logger.LogError("Viewer not found for connection: {ConnectionId}", connectionId);
            return;
        }

        var capturingContext = viewer.CapturingContext;
        if (capturingContext.SelectedScreen != null &&
            capturingContext.SelectedScreen.Equals(display))
        {
            logger.LogInformation("Selected screen is already set for connection: {ConnectionId}", connectionId);
            return;
        }

        capturingContext.SelectedScreen = display;
        logger.LogInformation("Selected screen set to {ScreenName} for connection: {ConnectionId}", display.DeviceName, connectionId);
    }

    /// <summary>
    /// Returns a thumbnail image (as a byte array) for the connection.
    /// </summary>
    public virtual byte[]? GetThumbnail(string connectionId)
    {
        var targetScreen = screenProvider.GetAllScreens().Count() > 1
            ? screenProvider.GetVirtualScreen()
            : screenProvider.GetPrimaryScreen();

        if (targetScreen == null)
        {
            logger.LogError("No screen available for thumbnail generation.");
            return null;
        }

        // Capture with lower quality for thumbnail.
        return CaptureScreen(connectionId, targetScreen.Bounds, imageQuality: 50, codec: "image/png");
    }

    public virtual void Dispose()
    {
        // Dispose managed resources if needed.
    }
}
