// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using System.Net.NetworkInformation;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class WakeOnLanService(IPacketSender packetSender) : IWakeOnLanService
{
    public void WakeUp(PhysicalAddress macAddress, int port = 9)
    {
        var packet = Enumerable.Repeat((byte)0xFF, 6)
            .Concat(Enumerable.Repeat(macAddress.GetAddressBytes(), 16).SelectMany(b => b))
            .ToArray();

        packetSender.Send(packet, new IPEndPoint(IPAddress.Broadcast, port));
    }
}
