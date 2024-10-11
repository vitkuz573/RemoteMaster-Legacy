// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using FluentResults;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Services;

public class UdpPacketSender(Func<IUdpClient> udpClientFactory, ILogger<UdpPacketSender> logger) : IPacketSender
{
    /// <inheritdoc />
    public Result Send(byte[] packet, IPEndPoint endPoint)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(packet);

            using var client = udpClientFactory();
            client.EnableBroadcast = true;

            client.Send(packet, packet.Length, endPoint);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while sending a UDP packet.");

            return Result.Fail("An error occurred while sending a UDP packet.").WithError(ex.Message);
        }
    }
}
