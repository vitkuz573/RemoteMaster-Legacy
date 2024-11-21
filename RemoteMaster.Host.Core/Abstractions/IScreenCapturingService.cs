// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Drawing;
using RemoteMaster.Host.Core.EventArguments;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IScreenCapturingService : IDisposable
{
    bool DrawCursor { get; set; }

    int ImageQuality { get; set; }

    string? SelectedCodec { get; set; }

    event EventHandler<ScreenChangedEventArgs> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    bool ShowClickIndicator { get; set; }

    byte[]? GetNextFrame();

    IEnumerable<Display> GetDisplays();

    void SetSelectedScreen(string displayName);

    byte[]? GetThumbnail(int maxWidth, int maxHeight);
}