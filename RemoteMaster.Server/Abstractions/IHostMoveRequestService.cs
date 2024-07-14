// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Abstractions;

public interface IHostMoveRequestService
{
    Task<List<HostMoveRequest>> GetHostMoveRequestsAsync();

    Task SaveHostMoveRequestsAsync(List<HostMoveRequest> hostMoveRequests);

    Task<HostMoveRequest?> GetHostMoveRequestAsync(string macAddress);

    Task AcknowledgeMoveRequestAsync(string macAddress);
}
