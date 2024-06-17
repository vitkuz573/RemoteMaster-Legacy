// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class UdpPacketSender(Func<IUdpClient> udpClientFactory) : IPacketSender
{
    public void Send(byte[] packet, IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(packet);

        using var client = udpClientFactory();
        client.EnableBroadcast = true;

        client.Send(packet, packet.Length, endPoint);
    }
}
