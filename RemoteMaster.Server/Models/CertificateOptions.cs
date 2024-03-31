// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Models;

public class CertificateOptions
{
    public int RSAKeySize { get; init; }

    public int ValidityPeriod { get; init; }

    public string CommonName { get; init; }

    public SubjectOptions Subject { get; init; }
}
