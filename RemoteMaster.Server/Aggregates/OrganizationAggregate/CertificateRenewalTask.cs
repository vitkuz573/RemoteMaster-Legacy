// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class CertificateRenewalTask
{
    private CertificateRenewalTask() { }

    internal CertificateRenewalTask(Host host, Organization organization, DateTimeOffset plannedDate)
    {
        if (plannedDate <= DateTimeOffset.Now)
        {
            throw new ArgumentException("Planned date must be in the future.", nameof(plannedDate));
        }

        Id = Guid.NewGuid();
        Host = host ?? throw new ArgumentNullException(nameof(host));
        HostId = host.Id;
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationId = organization.Id;
        OrganizationalUnit = host.Parent ?? throw new ArgumentNullException(nameof(host));
        OrganizationalUnitId = host.ParentId;
        PlannedDate = plannedDate;
        Status = CertificateRenewalStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid HostId { get; private set; }

    public Host Host { get; private set; } = null!;

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; } = null!;

    public DateTimeOffset PlannedDate { get; private set; }

    public DateTimeOffset? LastAttemptDate { get; private set; }

    public CertificateRenewalStatus Status { get; private set; }

    internal void MarkCompleted()
    {
        Status = CertificateRenewalStatus.Completed;
        LastAttemptDate = DateTimeOffset.Now;
    }

    internal void MarkFailed()
    {
        Status = CertificateRenewalStatus.Failed;
        LastAttemptDate = DateTimeOffset.Now;
    }
}
