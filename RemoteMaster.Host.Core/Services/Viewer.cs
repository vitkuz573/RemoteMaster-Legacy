// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class Viewer(IScreenCapturerService screenCapturer, string connectionId, string group, string userName, string role) : IViewer
{
    public IScreenCapturerService ScreenCapturer { get; } = screenCapturer;

    public string Group { get; } = group;

    public string ConnectionId { get; } = connectionId;

    public string UserName { get; } = userName;

    public string Role { get; } = role;

    public DateTime ConnectedTime { get; } = DateTime.UtcNow;

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public void Dispose()
    {
        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();
    }
}
