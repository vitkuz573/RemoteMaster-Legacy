﻿// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Server.Aggregates.CrlAggregate.ValueObjects;

namespace RemoteMaster.Server.Aggregates.CrlAggregate;

public class Crl : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private readonly List<RevokedCertificate> _revokedCertificates = [];

    private Crl() { }

    public Crl(string number)
    {
        Number = number;
    }

    public int Id { get; private set; }

    public string Number { get; private set; } = null!;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<RevokedCertificate> RevokedCertificates => _revokedCertificates.AsReadOnly();

    public void RevokeCertificate(SerialNumber serialNumber, X509RevocationReason reason)
    {
        ArgumentNullException.ThrowIfNull(serialNumber);

        if (_revokedCertificates.Any(rc => rc.SerialNumber.Equals(serialNumber)))
        {
            throw new InvalidOperationException($"Certificate with serial number {serialNumber.Value} is already revoked.");
        }

        var revokedCertificate = new RevokedCertificate(serialNumber, reason);

        _revokedCertificates.Add(revokedCertificate);
    }

    public void SetNumber(string number)
    {
        Number = number;
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
