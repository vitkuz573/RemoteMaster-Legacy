// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Abstractions;

public interface IViewer : IDisposable
{
    IScreenCapturingService ScreenCapturing { get; }

    string ConnectionId { get; }

    int FrameRate { get; set; }

    string Group { get; }

    string UserName { get; }

    string Role { get; }

    DateTime ConnectedTime { get; }

    public string IpAddress { get; }

    public string AuthenticationType { get; }

    CancellationTokenSource CancellationTokenSource { get; }
}
