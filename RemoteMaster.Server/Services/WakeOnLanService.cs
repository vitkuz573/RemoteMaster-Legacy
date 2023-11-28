// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Helpers;

namespace RemoteMaster.Server.Services;

public class WakeOnLanService(IPacketSender packetSender) : IWakeOnLanService
{
    private const int DefaultPort = 9;

    public void WakeUp(string macAddress)
    {
        var packet = MagicPacketCreator.Create(macAddress);
        packetSender.Send(packet, new IPEndPoint(IPAddress.Broadcast, DefaultPort));
    }
}
