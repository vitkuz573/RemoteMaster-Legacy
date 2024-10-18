// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate;

public class CertificateRenewalTask : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

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

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
