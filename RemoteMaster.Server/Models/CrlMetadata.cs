// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.DTOs;

namespace RemoteMaster.Server.Models;

public class CrlMetadata(CrlInfoDto crlInfo, int revokedCertificatesCount)
{
    public CrlInfoDto CrlInfo { get; } = crlInfo;

    public int RevokedCertificatesCount { get; } = revokedCertificatesCount;
}