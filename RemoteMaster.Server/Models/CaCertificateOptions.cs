// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class CaCertificateOptions
{
    public string Name { get; init; }

    public string PfxPath { get; init; }

    public string PfxPassword { get; init; }
}
