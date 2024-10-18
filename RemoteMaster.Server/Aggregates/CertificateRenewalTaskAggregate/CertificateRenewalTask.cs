// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate.ValueObjects;
using RemoteMaster.Server.Enums;

namespace RemoteMaster.Server.Aggregates.CertificateRenewalTaskAggregate;

public class CertificateRenewalTask : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private CertificateRenewalTask() { }

    internal CertificateRenewalTask(Guid hostId, RenewalSchedule renewalSchedule)
    {
        Id = Guid.NewGuid();
        HostId = hostId;
        RenewalSchedule = renewalSchedule;
        Status = CertificateRenewalStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid HostId { get; private set; }

    public RenewalSchedule RenewalSchedule { get; private set; }

    public CertificateRenewalStatus Status { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void SetStatus(CertificateRenewalStatus status)
    {
        Status = status;
    }

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
