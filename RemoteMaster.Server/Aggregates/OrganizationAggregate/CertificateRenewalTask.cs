// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.OrganizationAggregate;

public class CertificateRenewalTask
{
    public Guid Id { get; set; }

    public Guid ComputerId { get; set; }

    public Computer Computer { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization Organization { get; set; }

    public DateTime PlannedDate { get; set; }

    public DateTime? LastAttemptDate { get; set; }

    public CertificateRenewalStatus Status { get; set; }
}
