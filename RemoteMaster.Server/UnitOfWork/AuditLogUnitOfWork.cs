// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Data;

namespace RemoteMaster.Server.UnitOfWork;

public class AuditLogUnitOfWork(AuditLogDbContext context, IDomainEventDispatcher domainEventDispatcher, IAuditLogRepository auditLogRepository, ILogger<UnitOfWork<AuditLogDbContext>> logger) : UnitOfWork<AuditLogDbContext>(context, domainEventDispatcher, logger), IAuditLogUnitOfWork
{
    public IAuditLogRepository AuditLogs { get; } = auditLogRepository;
}
