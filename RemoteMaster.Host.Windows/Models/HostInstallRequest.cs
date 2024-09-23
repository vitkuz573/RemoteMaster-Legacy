// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Shared.Models;

namespace RemoteMaster.Host.Windows.Models;

public class HostInstallRequest(string server, string organization, string organizationalUnit)
{
    public string Server { get; } = server;

    public string Organization { get; } = organization;

    public string OrganizationalUnit { get; } = organizationalUnit;

    public string? ModulesPath { get; init; }

    public Credentials? UserCredentials { get; set; }
}
