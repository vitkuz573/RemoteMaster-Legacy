// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Host.Windows.Abstractions;
using RemoteMaster.Host.Windows.Helpers.ScreenHelper;
using RemoteMaster.Host.Windows.ScreenOverlays;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Services;

public abstract class ScreenCapturingService : IScreenCapturingService
{
    protected const string VirtualScreen = "VIRTUAL_SCREEN";

    private readonly IDesktopService _desktopService;
    private readonly ILogger<ScreenCapturingService> _logger;
    private readonly Lock _screenBoundsLock = new();

    private readonly IEnumerable<IScreenOverlay> _availableOverlays;
    private readonly List<IScreenOverlay> _activeOverlays = [];

    public bool DrawCursor
    {
        get => _activeOverlays.Any(o => o.Name == nameof(CursorOverlay));
        set
        {
            if (value == DrawCursor)
            {
                return;
            }

            if (value)
            {
                ActivateOverlay(nameof(CursorOverlay));
            }
            else
            {
                DeactivateOverlay(nameof(CursorOverlay));
            }
        }
    }

    public int ImageQuality { get; set; } = 25;

    public string? SelectedCodec { get; set; } = "image/jpeg";

    protected Dictionary<string, int> Screens { get; } = [];

    public Rectangle CurrentScreenBounds { get; protected set; } = Screen.PrimaryScreen?.Bounds ?? Rectangle.Empty;

    public Rectangle VirtualScreenBounds { get; } = SystemInformation.VirtualScreen;

    public string SelectedScreen { get; protected set; } = Screen.PrimaryScreen?.DeviceName ?? string.Empty;

    private static bool HasMultipleScreens => Screen.AllScreens.Length > 1;

    public bool ShowClickIndicator
    {
        get => _activeOverlays.Any(o => o.Name == nameof(ClickIndicatorOverlay));
        set
        {
            if (value == ShowClickIndicator)
            {
                return;
            }

            if (value)
            {
                ActivateOverlay(nameof(ClickIndicatorOverlay));
            }
            else
            {
                DeactivateOverlay(nameof(ClickIndicatorOverlay));
            }
        }
    }

    public event EventHandler<ScreenChangedEventArgs>? ScreenChanged;

    protected ScreenCapturingService(IDesktopService desktopService, IEnumerable<IScreenOverlay> availableOverlays, ILogger<ScreenCapturingService> logger)
    {
        _desktopService = desktopService;
        _availableOverlays = availableOverlays;
        _logger = logger;

        Init();
    }

    protected abstract void Init();

    protected abstract byte[]? GetFrame();

    private void ActivateOverlay(string name)
    {
        var overlay = _availableOverlays.FirstOrDefault(o => o.Name == name);

        if (overlay != null && !_activeOverlays.Contains(overlay))
        {
            _activeOverlays.Add(overlay);
        }
    }

    private void DeactivateOverlay(string name)
    {
        var overlay = _activeOverlays.FirstOrDefault(o => o.Name == name);

        if (overlay != null)
        {
            _activeOverlays.Remove(overlay);
        }
    }

    public IEnumerable<IScreenOverlay> GetActiveOverlays() => _activeOverlays;

    public IEnumerable<Display> GetDisplays()
    {
        var screens = Screen.AllScreens.Select(screen => new Display
        {
            Name = screen.DeviceName,
            IsPrimary = screen.Primary,
            Resolution = screen.Bounds.Size,
        }).ToList();

        if (Screen.AllScreens.Length > 1)
        {
            screens.Add(new Display
            {
                Name = VirtualScreen,
                IsPrimary = false,
                Resolution = new Size(VirtualScreenBounds.Width, VirtualScreenBounds.Height),
            });
        }

        return screens;
    }

    public abstract void SetSelectedScreen(string displayName);

    protected abstract void RefreshCurrentScreenBounds();

    public byte[]? GetNextFrame()
    {
        using (_screenBoundsLock.EnterScope())
        {
            try
            {
                RefreshCurrentScreenBounds();

                if (!_desktopService.SwitchToInputDesktop())
                {
                    _logger.LogDebug("Failed to switch to input desktop. Last Win32 error code: {ErrorCode}", Marshal.GetLastWin32Error());
                }

                return GetFrame();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting next frame.");

                return null;
            }
        }
    }

    public byte[]? GetThumbnail(int maxWidth, int maxHeight)
    {
        var originalScreen = SelectedScreen;

        if (HasMultipleScreens)
        {
            SetSelectedScreen(VirtualScreen);
        }

        var frame = GetNextFrame();

        SetSelectedScreen(originalScreen);

        return frame ?? null;
    }

    protected void RaiseScreenChangedEvent(Rectangle currentScreenBounds)
    {
        ScreenChanged?.Invoke(this, new ScreenChangedEventArgs(currentScreenBounds));
    }

    public virtual void Dispose()
    {
    }
}
