// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public interface IViewer : IDisposable
{
    IScreenCapturerService ScreenCapturer { get; }

    string ConnectionId { get; }

    Task StartStreaming();

    void StopStreaming();

    Task SendDisplays(IEnumerable<Display> displays);

    Task SendScreenSize(int width, int height);

    void SetSelectedScreen(string displayName);
}
