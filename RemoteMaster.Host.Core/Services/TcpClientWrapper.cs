// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.Sockets;
using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class TcpClientWrapper : ITcpClient
{
    private readonly TcpClient _tcpClient = new();

    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
    {
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
    }

    public void Dispose()
    {
        _tcpClient.Dispose();
    }
}
