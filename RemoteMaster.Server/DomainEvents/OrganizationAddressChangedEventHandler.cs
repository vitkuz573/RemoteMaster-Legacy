// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.AuditLogAggregate;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationAddressChangedEventHandler(IApplicationUnitOfWork applicationUnitOfWork, IAuditLogUnitOfWork auditLogUnitOfWork, ICertificateTaskUnitOfWork certificateTaskUnitOfWork, ICurrentUserService currentUserService, ILogger<OrganizationAddressChangedEventHandler> logger) : IDomainEventHandler<OrganizationAddressChangedEvent>
{
    public async Task HandleAsync(OrganizationAddressChangedEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        logger.LogInformation("Organization {OrganizationId} changed address to {NewAddress}", domainEvent.OrganizationId, domainEvent.NewAddress);

        var organization = await applicationUnitOfWork.Organizations.GetByIdAsync(domainEvent.OrganizationId);

        if (organization != null)
        {
            foreach (var unit in organization.OrganizationalUnits)
            {
                foreach (var host in unit.Hosts)
                {
                    var task = host.CreateCertificateRenewalTask(DateTimeOffset.UtcNow.AddHours(1));

                    await certificateTaskUnitOfWork.CertificateRenewalTasks.AddAsync(task);
                }
            }

            await certificateTaskUnitOfWork.CommitAsync(ct);
        }

        await AddAuditLogAsync(domainEvent, organization, ct);
    }

    private async Task AddAuditLogAsync(OrganizationAddressChangedEvent domainEvent, Organization? organization, CancellationToken ct)
    {
        var userName = currentUserService.UserName;

        if (organization == null)
        {
            logger.LogWarning("Organization with ID {OrganizationId} not found. Audit log not created.", domainEvent.OrganizationId);
            
            return;
        }

        var details = $"Organization '{organization.Name}' (ID: {organization.Id}) changed address to '{domainEvent.NewAddress}'.";

        var auditLog = AuditLog.Create("OrganizationAddressChanged", userName, details);

        await auditLogUnitOfWork.AuditLogs.AddAsync(auditLog);
        await auditLogUnitOfWork.CommitAsync(ct);

        logger.LogInformation("Audit log created for Organization {OrganizationId} address change.", domainEvent.OrganizationId);
    }
}
