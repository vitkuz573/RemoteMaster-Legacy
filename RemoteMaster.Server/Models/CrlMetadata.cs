// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class CrlMetadata
{
    public CrlInfo CrlInfo { get; set; }

    public int RevokedCertificatesCount { get; set; }
}