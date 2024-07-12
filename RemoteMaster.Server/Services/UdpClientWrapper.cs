// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class UdpClientWrapper : IUdpClient
{
    private readonly UdpClient _udpClient = new();

    public bool EnableBroadcast
    {
        get => _udpClient.EnableBroadcast;
        set => _udpClient.EnableBroadcast = value;
    }

    public int Send(byte[] datagram, int bytes, IPEndPoint endPoint)
    {
        return _udpClient.Send(datagram, bytes, endPoint);
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }
}