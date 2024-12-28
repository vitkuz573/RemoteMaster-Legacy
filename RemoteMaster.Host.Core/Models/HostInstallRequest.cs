// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Host.Core.Models;

public class HostInstallRequest(string server, string organization, List<string> organizationalUnit, bool force)
{
    public string Server { get; } = server;

    public string Organization { get; } = organization;

    public List<string> OrganizationalUnit { get; } = organizationalUnit;

    public bool Force { get; } = force;
}
