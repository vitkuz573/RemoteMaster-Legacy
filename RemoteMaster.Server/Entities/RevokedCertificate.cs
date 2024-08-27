// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Server.Entities;

public class RevokedCertificate
{
    private RevokedCertificate() { }

    public RevokedCertificate(string serialNumber, X509RevocationReason reason)
    {
        SerialNumber = serialNumber ?? throw new ArgumentNullException(nameof(serialNumber));
        Reason = reason;
        RevocationDate = DateTimeOffset.UtcNow;
    }

    public int Id { get; private set; }

    public string SerialNumber { get; private set; }
    
    public X509RevocationReason Reason { get; private set; }

    public DateTimeOffset RevocationDate { get; private set; }
}
