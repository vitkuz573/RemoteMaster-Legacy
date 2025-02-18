// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationalUnitCreatedEventHandler(ICurrentUserService currentUserService, IAuditLogUnitOfWork auditLogUnitOfWork, ILogger<OrganizationalUnitCreatedEventHandler> logger) : IDomainEventHandler<OrganizationalUnitCreatedEvent>
{
    public async Task HandleAsync(OrganizationalUnitCreatedEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        logger.LogInformation("Organizational unit '{OrganizationalUnitName}' (ID: {OrganizationalUnitId}) was created.", domainEvent.Name, domainEvent.OrganizationalUnitId);

        await AddAuditLogAsync(domainEvent, ct);
    }

    private async Task AddAuditLogAsync(OrganizationalUnitCreatedEvent domainEvent, CancellationToken ct)
    {
        var userName = currentUserService.UserName;

        var details = $"OrganizationalUnit '{domainEvent.Name}' (ID: {domainEvent.OrganizationId}) was created.";

        var auditLog = AuditLog.Create("OrganizationalUnitCreated", userName, details);

        await auditLogUnitOfWork.AuditLogs.AddAsync(auditLog);
        await auditLogUnitOfWork.CommitAsync(ct);

        logger.LogInformation("Audit log created for OrganizationalUnit '{OrganizationalUnitId}' creation.", domainEvent.OrganizationId);
    }
}
