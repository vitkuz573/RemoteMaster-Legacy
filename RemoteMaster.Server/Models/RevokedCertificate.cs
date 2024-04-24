// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace RemoteMaster.Server.Models;

public class RevokedCertificate
{
    public int Id { get; set; }

    [Required]
    public string SerialNumber { get; set; }

    [Required]
    public X509RevocationReason Reason { get; set; }

    [Required]
    public DateTimeOffset RevocationDate { get; set; }
}
