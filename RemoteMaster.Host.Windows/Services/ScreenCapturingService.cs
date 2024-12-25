// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Host.Windows.ScreenOverlays;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Services;

public abstract class ScreenCapturingService : IScreenCapturingService
{
    private readonly IAppState _appState;
    private readonly IDesktopService _desktopService;
    private readonly IOverlayManagerService _overlayManagerService;
    private readonly ILogger<ScreenCapturingService> _logger;
    private readonly Lock _screenBoundsLock = new();

    private readonly ConcurrentDictionary<string, EventHandler> _isCursorVisibleHandlers = new();

    private static bool HasMultipleScreens => Screen.AllScreens.Length > 1;

    protected ScreenCapturingService(IAppState appState, IDesktopService desktopService, IOverlayManagerService overlayManagerService, ILogger<ScreenCapturingService> logger)
    {
        _appState = appState;
        _desktopService = desktopService;
        _overlayManagerService = overlayManagerService;
        _logger = logger;

        _appState.ViewerAdded += OnViewerAdded;
        _appState.ViewerRemoved += OnViewerRemoved;

        foreach (var viewer in _appState.GetAllViewers())
        {
            SubscribeToViewer(viewer);
        }
    }

    private byte[]? GetFrame(string connectionId)
    {
        try
        {
            if (!_appState.TryGetViewer(connectionId, out var viewer) || viewer?.CapturingContext.SelectedScreen == null)
            {
                _logger.LogWarning("Viewer not found or SelectedScreen is null for ConnectionId: {ConnectionId}", connectionId);

                return null;
            }

            var capturingContext = viewer.CapturingContext;

            return capturingContext.SelectedScreen.DeviceName == Screen.VirtualScreen.DeviceName
                ? GetVirtualScreenFrame(connectionId, capturingContext.ImageQuality, capturingContext.SelectedCodec)
                : GetSingleScreenFrame(connectionId, capturingContext.SelectedScreen.Bounds, capturingContext.ImageQuality, capturingContext.SelectedCodec);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred in GetFrame for ConnectionId: {ConnectionId}", connectionId);

            return null;
        }
    }

    private byte[] GetVirtualScreenFrame(string connectionId, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, Screen.VirtualScreen.Bounds, imageQuality, codec);
    }

    private byte[] GetSingleScreenFrame(string connectionId, Rectangle bounds, int imageQuality, string codec)
    {
        return CaptureScreen(connectionId, bounds, imageQuality, codec);
    }

    public IEnumerable<Display> GetDisplays()
    {
        var screens = Screen.AllScreens
            .Select(screen => new Display
            {
                Name = screen.DeviceName,
                IsPrimary = screen.Primary,
                Resolution = screen.Bounds.Size,
            })
            .ToList();

        if (Screen.AllScreens.Length > 1)
        {
            screens.Add(new Display
            {
                Name = Screen.VirtualScreen.DeviceName,
                IsPrimary = false,
                Resolution = Screen.VirtualScreen.Bounds.Size,
            });
        }

        return screens;
    }

    public IScreen? FindScreenByName(string displayName)
    {
        var allScreens = Screen.AllScreens.ToList();

        if (HasMultipleScreens)
        {
            allScreens.Add(Screen.VirtualScreen);
        }

        return allScreens.FirstOrDefault(screen => screen.DeviceName == displayName);
    }

    protected abstract byte[] CaptureScreen(string connectionId, Rectangle bounds, int imageQuality, string codec);

    public void SetSelectedScreen(string connectionId, IScreen display)
    {
        ArgumentNullException.ThrowIfNull(display);

        if (!_appState.TryGetViewer(connectionId, out var viewer) || viewer == null)
        {
            _logger.LogError("Viewer not found for ConnectionId: {ConnectionId}", connectionId);

            return;
        }

        var capturingContext = viewer.CapturingContext;

        if (capturingContext.SelectedScreen != null && capturingContext.SelectedScreen.Equals(display))
        {
            _logger.LogInformation("SelectedScreen is already set to {ScreenName} for ConnectionId: {ConnectionId}. No action taken.", display.DeviceName, connectionId);

            return;
        }

        capturingContext.SelectedScreen = display;

        _logger.LogInformation("SelectedScreen set to {ScreenName} for ConnectionId: {ConnectionId}", display.DeviceName, connectionId);
    }

    public byte[]? GetNextFrame(string connectionId)
    {
        using (_screenBoundsLock.EnterScope())
        {
            try
            {
                _appState.TryGetViewer(connectionId, out var viewer);

                var capturingContext = viewer?.CapturingContext;

                if (capturingContext?.SelectedScreen != null)
                {
                    if (!_desktopService.SwitchToInputDesktop())
                    {
                        _logger.LogDebug("Failed to switch to input desktop. Last Win32 error code: {ErrorCode}", Marshal.GetLastWin32Error());
                    }

                    return GetFrame(connectionId);
                }

                _logger.LogWarning("Selected screen is null for connection ID {ConnectionId}", connectionId);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");

                return null;
            }
        }
    }

    public byte[]? GetThumbnail(string connectionId)
    {
        var targetScreen = HasMultipleScreens ? Screen.VirtualScreen : Screen.PrimaryScreen;

        if (targetScreen == null)
        {
            _logger.LogError("No screens available for thumbnail generation.");

            return null;
        }

        SetSelectedScreen(connectionId, targetScreen);

        return GetNextFrame(connectionId);
    }

    protected static byte[] BitmapToByteArray(Bitmap bitmap, int quality, string? codec)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var memoryStream = new MemoryStream();
        using var encoderParameters = new EncoderParameters(1);
        encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);

        var imageCodec = GetEncoderInfo(codec);

        if (imageCodec != null)
        {
            bitmap.Save(memoryStream, imageCodec, encoderParameters);
        }
        else
        {
            var pngCodec = GetEncoderInfo("image/png");

            if (pngCodec != null)
            {
                bitmap.Save(memoryStream, pngCodec, null);
            }
            else
            {
                throw new InvalidOperationException("No suitable codec found");
            }
        }

        return memoryStream.ToArray();
    }

    private static ImageCodecInfo? GetEncoderInfo(string? mimeType)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();

        return codecs.FirstOrDefault(codec => codec.MimeType == mimeType);
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
            _overlayManagerService.ActivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
    }

    private void UnsubscribeFromViewer(IViewer viewer)
    {
        var context = viewer.CapturingContext;

        if (_isCursorVisibleHandlers.TryRemove(viewer.ConnectionId, out var handler))
        {
            context.OnIsCursorVisibleChanged -= handler;
        }

        _overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
    }

    private void HandleIsCursorVisibleChanged(IViewer? viewer)
    {
        if (viewer == null)
        {
            _logger.LogWarning("Viewer is null when handling IsCursorVisibleChanged event.");

            return;
        }

        if (viewer.CapturingContext.IsCursorVisible)
        {
            _overlayManagerService.ActivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
        else
        {
            _overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), viewer.ConnectionId);
        }
    }

    public virtual void Dispose()
    {
        _appState.ViewerAdded -= OnViewerAdded;
        _appState.ViewerRemoved -= OnViewerRemoved;

        foreach (var viewer in _appState.GetAllViewers())
        {
            UnsubscribeFromViewer(viewer);
        }
    }
}
