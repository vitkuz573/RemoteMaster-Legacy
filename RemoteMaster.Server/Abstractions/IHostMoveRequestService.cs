// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Net.NetworkInformation;
using FluentResults;
using RemoteMaster.Server.Aggregates.HostMoveRequestAggregate;

namespace RemoteMaster.Server.Abstractions;

/// <summary>
/// Interface for managing host move requests.
/// </summary>
public interface IHostMoveRequestService
{
    /// <summary>
    /// Retrieves a specific host move request by MAC address.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <returns>The <see cref="HostMoveRequest"/> wrapped in a <see cref="Result{T}"/>.</returns>
    Task<Result<HostMoveRequest?>> GetHostMoveRequestAsync(PhysicalAddress macAddress);

    /// <summary>
    /// Acknowledges a move request by removing it and sending a notification.
    /// </summary>
    /// <param name="macAddress">The MAC address of the host.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    Task<Result> AcknowledgeMoveRequestAsync(PhysicalAddress macAddress);
}
