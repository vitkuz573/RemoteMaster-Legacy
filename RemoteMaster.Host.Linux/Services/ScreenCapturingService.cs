// DummyScreenCapturingService.cs
// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Linux.Services;

/// <summary>
/// A dummy implementation of IScreenCapturingService.
/// Returns default values without throwing exceptions.
/// </summary>
public class ScreenCapturingService : IScreenCapturingService
{
    /// <summary>
    /// Returns a dummy frame (null in this case).
    /// </summary>
    public byte[]? GetNextFrame(string connectionId)
    {
        // Return null or a default byte array as needed.
        return null;
    }

    /// <summary>
    /// Returns an empty list of displays.
    /// </summary>
    public IEnumerable<Display> GetDisplays()
    {
        return new List<Display>();
    }

    /// <summary>
    /// Always returns null for finding a screen by name.
    /// </summary>
    public IScreen? FindScreenByName(string displayName)
    {
        return null;
    }

    /// <summary>
    /// Does nothing in this dummy implementation.
    /// </summary>
    public void SetSelectedScreen(string connectionId, IScreen display)
    {
        // No operation.
    }

    /// <summary>
    /// Returns a dummy thumbnail (same as GetNextFrame).
    /// </summary>
    public byte[]? GetThumbnail(string connectionId)
    {
        return GetNextFrame(connectionId);
    }

    /// <summary>
    /// Cleans up any resources (none in this dummy implementation).
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose.
    }
}