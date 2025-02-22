// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class HostMoveRequestUnitOfWork(HostMoveRequestDbContext context, IDomainEventDispatcher domainEventDispatcher, IHostMoveRequestRepository hostMoveRequestRepository, ILogger<UnitOfWork<HostMoveRequestDbContext>> logger) : UnitOfWork<HostMoveRequestDbContext>(context, domainEventDispatcher, logger), IHostMoveRequestUnitOfWork
{
    public IHostMoveRequestRepository HostMoveRequests { get; } = hostMoveRequestRepository;
}
