// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class Viewer(IScreenCapturingService screenCapturing, string connectionId, string group, string userName, string role, string ipAddress, string authenticationType) : IViewer
{
    public IScreenCapturingService ScreenCapturing { get; } = screenCapturing;

    public int FrameRate { get; set; }

    public string Group { get; } = group;

    public string ConnectionId { get; } = connectionId;

    public string UserName { get; } = userName;

    public string Role { get; } = role;

    public DateTime ConnectedTime { get; } = DateTime.UtcNow;

    public string IpAddress { get; } = ipAddress;

    public string AuthenticationType { get; } = authenticationType;

    public CancellationTokenSource CancellationTokenSource { get; } = new();

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CancellationTokenSource.Cancel();
        CancellationTokenSource.Dispose();

        _disposed = true;
    }
}