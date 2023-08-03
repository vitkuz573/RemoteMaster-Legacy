// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.

using System.Drawing;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Core.Abstractions;

public interface IScreenCapturer : IDisposable
{
    bool TrackCursor { get; set; }

    int Quality { get; set; }

    event EventHandler<Rectangle> ScreenChanged;

    Rectangle CurrentScreenBounds { get; }

    Rectangle VirtualScreenBounds { get; }

    string SelectedScreen { get; }

    byte[]? GetNextFrame();

    IEnumerable<DisplayInfo> GetDisplays();

    void SetSelectedScreen(string displayName);
}