// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.Sockets;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class UdpPacketSender : IPacketSender
{
    public void Send(byte[] packet, IPEndPoint endPoint)
    {
        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }    

        using var client = new UdpClient()
        {
            EnableBroadcast = true
        };

        client.Send(packet, packet.Length, endPoint);
    }
}
