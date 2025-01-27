// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.DomainEvents;

public class OrganizationAddressChangedEventHandler(IApplicationUnitOfWork applicationUnitOfWork, ICertificateTaskUnitOfWork certificateTaskUnitOfWork, ILogger<OrganizationAddressChangedEventHandler> logger) : IDomainEventHandler<OrganizationAddressChangedEvent>
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
    }
}
