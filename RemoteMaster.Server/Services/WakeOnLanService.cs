// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Helpers;

namespace RemoteMaster.Server.Services;

public class WakeOnLanService : IWakeOnLanService
{
    private readonly IPacketSender _packetSender;
    private const int DefaultPort = 9;

    public WakeOnLanService(IPacketSender packetSender)
    {
        _packetSender = packetSender;
    }

    public void WakeUp(string macAddress)
    {
        var packet = MagicPacketCreator.Create(macAddress);
        _packetSender.Send(packet, new IPEndPoint(IPAddress.Broadcast, DefaultPort));
    }
}
