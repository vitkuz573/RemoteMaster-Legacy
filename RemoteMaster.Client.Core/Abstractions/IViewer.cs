// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Client.Core.Abstractions;

public interface IViewer
{
    IScreenCapturerService ScreenCapturer { get; }

    string ConnectionId { get; }

    Task StartStreaming();

    void StopStreaming();

    Task SendScreenData(IEnumerable<DisplayInfo> displays, int screenWidth, int screenHeight);

    Task SendScreenSize(int width, int height);

    void SetSelectedScreen(string displayName);
}
