// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

namespace RemoteMaster.Server.Models;

public class UserInfo
{
    public string UserName { get; set; } = "UnknownUser";

    public IList<string> Roles { get; } = [];

#pragma warning disable CA2227
    public List<Guid> AccessibleOrganizations { get; set; } = [];

    public List<Guid> AccessibleOrganizationalUnits { get; set; } = [];
#pragma warning restore CA2227
}
