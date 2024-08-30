// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;
using RemoteMaster.Server.Abstractions;

namespace RemoteMaster.Server.Entities;

public class Crl : IAggregateRoot
{
    private readonly List<RevokedCertificate> _revokedCertificates = [];

    private Crl() { }

    public Crl(string number)
    {
        Number = number;
    }

    public int Id { get; private set; }

    public string Number { get; private set; }

    public IReadOnlyCollection<RevokedCertificate> RevokedCertificates => _revokedCertificates.AsReadOnly();

    public void RevokeCertificate(string serialNumber, X509RevocationReason reason)
    {
        if (_revokedCertificates.Any(rc => rc.SerialNumber == serialNumber))
        {
            throw new InvalidOperationException($"Certificate with serial number {serialNumber} is already revoked.");
        }

        var revokedCertificate = new RevokedCertificate(serialNumber, reason);
        
        _revokedCertificates.Add(revokedCertificate);
    }

    public void SetNumber(string number)
    {
        Number = number;
    }
}
