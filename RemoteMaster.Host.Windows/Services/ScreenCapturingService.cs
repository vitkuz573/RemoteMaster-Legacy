// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
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

    protected Dictionary<string, int> Screens { get; } = [];

    private static bool HasMultipleScreens => Screen.AllScreens.Length > 1;

    protected ScreenCapturingService(IAppState appState, IDesktopService desktopService, IOverlayManagerService overlayManagerService, ILogger<ScreenCapturingService> logger)
    {
        _appState = appState;
        _desktopService = desktopService;
        _overlayManagerService = overlayManagerService;
        _logger = logger;

        Init();

        _appState.CapturingContextAdded += OnCapturingContextAdded;
        _appState.CapturingContextRemoved += OnCapturingContextRemoved;

        foreach (var context in _appState.CapturingContexts.Values)
        {
            SubscribeToCapturingContext(context);
        }
    }

    protected abstract void Init();

    protected abstract byte[]? GetFrame(string connectionId);

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

    public abstract void SetSelectedScreen(string connectionId, IScreen display);

    public byte[]? GetNextFrame(string connectionId)
    {
        using (_screenBoundsLock.EnterScope())
        {
            try
            {
                _appState.TryGetCapturingContext(connectionId, out var capturingContext);

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

    private void OnCapturingContextAdded(object? sender, ICapturingContext? e)
    {
        if (e is CapturingContext context)
        {
            SubscribeToCapturingContext(context);
        }
    }

    private void OnCapturingContextRemoved(object? sender, ICapturingContext? e)
    {
        if (e is CapturingContext context)
        {
            UnsubscribeFromCapturingContext(context);
        }
    }

    private void SubscribeToCapturingContext(ICapturingContext context)
    {
        context.OnDrawCursorChanged += HandleDrawCursorChanged;

        if (context.DrawCursor)
        {
            _overlayManagerService.ActivateOverlay(nameof(CursorOverlay), context.ConnectionId);
        }
    }

    private void UnsubscribeFromCapturingContext(ICapturingContext context)
    {
        context.OnDrawCursorChanged -= HandleDrawCursorChanged;

        _overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), context.ConnectionId);
    }

    private void HandleDrawCursorChanged(object? sender, EventArgs e)
    {
        if (sender is not CapturingContext context)
        {
            return;
        }

        if (context.DrawCursor)
        {
            _overlayManagerService.ActivateOverlay(nameof(CursorOverlay), context.ConnectionId);
        }
        else
        {
            _overlayManagerService.DeactivateOverlay(nameof(CursorOverlay), context.ConnectionId);
        }
    }

    public virtual void Dispose()
    {
    }
}
