// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate.ValueObjects;

public record RenewalSchedule(DateTimeOffset PlannedDate, DateTimeOffset? LastAttemptDate)
{
    public RenewalSchedule(DateTimeOffset plannedDate) : this(plannedDate, null)
    {
        if (plannedDate <= DateTimeOffset.Now)
        {
            throw new ArgumentException("Planned date must be in the future.", nameof(plannedDate));
        }
    }

    public RenewalSchedule SetLastAttemptDate(DateTimeOffset lastAttemptDate) => this with { LastAttemptDate = lastAttemptDate };
}
