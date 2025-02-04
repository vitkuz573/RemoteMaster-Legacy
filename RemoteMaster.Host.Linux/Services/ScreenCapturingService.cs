// Copyright © 2023 Vitaly Kuzyaev.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Linux.Helpers.ScreenHelper;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// Abstract base class for screen capturing services.
/// Provides default implementations for display enumeration and other common methods.
/// </summary>
public abstract class ScreenCapturingService : IScreenCapturingService
{
    /// <summary>
    /// Retrieves the next captured frame as a byte array.
    /// </summary>
    /// <param name="connectionId">A connection identifier.</param>
    /// <returns>Raw frame data or null if unavailable.</returns>
    public abstract byte[]? GetNextFrame(string connectionId);

    /// <summary>
    /// Retrieves a collection of connected displays.
    /// </summary>
    public virtual IEnumerable<Display> GetDisplays()
    {
        return Screen.AllScreens.Select(s => new Display
        {
            Name = s.DeviceName,
            IsPrimary = s.Primary,
            Resolution = s.Bounds.Size
        });
    }

    /// <summary>
    /// Finds a display by its device name.
    /// </summary>
    public virtual IScreen? FindScreenByName(string displayName)
    {
        return Screen.AllScreens.FirstOrDefault(s => s.DeviceName == displayName);
    }

    /// <summary>
    /// Sets the selected screen for capture.
    /// Default implementation is a no‑op.
    /// </summary>
    public virtual void SetSelectedScreen(string connectionId, IScreen display)
    {
        // No-op by default.
    }

    /// <summary>
    /// Retrieves a thumbnail image.
    /// Default implementation returns a full‑sized frame.
    /// </summary>
    public virtual byte[]? GetThumbnail(string connectionId)
    {
        return GetNextFrame(connectionId);
    }

    /// <summary>
    /// Disposes resources used by the screen capturing service.
    /// </summary>
    public abstract void Dispose();
}
