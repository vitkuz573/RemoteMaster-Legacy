// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.BusinessProcesses;

public class CertificateRenewalTask
{
    private CertificateRenewalTask() { }

    internal CertificateRenewalTask(Guid hostId, DateTimeOffset plannedDate)
    {
        if (plannedDate <= DateTimeOffset.Now)
        {
            throw new ArgumentException("Planned date must be in the future.", nameof(plannedDate));
        }

        Id = Guid.NewGuid();
        HostId = hostId;
        PlannedDate = plannedDate;
        Status = CertificateRenewalStatus.Pending;
    }

    public Guid Id { get; set; }

    public Guid HostId { get; set; }

    public DateTimeOffset PlannedDate { get; set; }

    public DateTimeOffset? LastAttemptDate { get; set; }

    public CertificateRenewalStatus Status { get; set; }
}
