// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Helpers;

namespace RemoteMaster.Server.Services;

public class WakeOnLanService(IPacketSender packetSender) : IWakeOnLanService
{
    public void WakeUp(string macAddress, int port = 9)
    {
        var packet = MagicPacketCreator.Create(macAddress);
        packetSender.Send(packet, new IPEndPoint(IPAddress.Broadcast, port));
    }
}
