// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Server.Entities;

public class RevokedCertificate
{
    public int Id { get; set; }

    public string SerialNumber { get; set; }
    
    public X509RevocationReason Reason { get; set; }

    public DateTimeOffset RevocationDate { get; set; }
}
