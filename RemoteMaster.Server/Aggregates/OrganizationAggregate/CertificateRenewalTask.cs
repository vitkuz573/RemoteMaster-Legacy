// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class CertificateRenewalTask
{
    private CertificateRenewalTask() { }

    internal CertificateRenewalTask(Computer computer, Organization organization, DateTimeOffset plannedDate)
    {
        if (plannedDate <= DateTimeOffset.Now)
        {
            throw new ArgumentException("Planned date must be in the future.", nameof(plannedDate));
        }

        Id = Guid.NewGuid();
        Computer = computer ?? throw new ArgumentNullException(nameof(computer));
        ComputerId = computer.Id;
        Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        OrganizationId = organization.Id;
        OrganizationalUnit = computer.Parent ?? throw new ArgumentNullException(nameof(computer));
        OrganizationalUnitId = computer.ParentId;
        PlannedDate = plannedDate;
        Status = CertificateRenewalStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid ComputerId { get; private set; }

    public Computer Computer { get; private set; }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; }

    public Guid OrganizationalUnitId { get; private set; }

    public OrganizationalUnit OrganizationalUnit { get; private set; }

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
