// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationCreatedEventHandler(ICurrentUserService currentUserService, IAuditLogUnitOfWork auditLogUnitOfWork, ILogger<OrganizationAddressChangedEventHandler> logger) : IDomainEventHandler<OrganizationCreatedEvent>
{
    public async Task HandleAsync(OrganizationCreatedEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        logger.LogInformation("Organization '{OrganizationName}' (ID: {OrganizationId}) was created.", domainEvent.Name, domainEvent.OrganizationId);

        await AddAuditLogAsync(domainEvent, ct);
    }

    private async Task AddAuditLogAsync(OrganizationCreatedEvent domainEvent, CancellationToken ct)
    {
        var userName = currentUserService.UserName;

        var details = $"Organization '{domainEvent.Name}' (ID: {domainEvent.OrganizationId}) was created.";

        var auditLog = AuditLog.Create("OrganizationCreated", userName, details);

        await auditLogUnitOfWork.AuditLogs.AddAsync(auditLog);
        await auditLogUnitOfWork.CommitAsync(ct);

        logger.LogInformation("Audit log created for Organization '{OrganizationId}' creation.", domainEvent.OrganizationId);
    }
}
