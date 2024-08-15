// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net;
using FluentResults;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for sending packets to a specified endpoint.
/// </summary>
public interface IPacketSender
{
    /// <summary>
    /// Sends a packet to the specified endpoint.
    /// </summary>
    /// <param name="packet">The packet to be sent.</param>
    /// <param name="endPoint">The endpoint to which the packet is sent.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result Send(byte[] packet, IPEndPoint endPoint);
}
