// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Server.Entities;

namespace RemoteMaster.Server.Abstractions;

public interface ILimitChecker
{
    bool CanAddOrganization(IEnumerable<Organization> organizations);

    bool CanAddOrganizationalUnit(Organization organization);

    bool CanAddComputer(OrganizationalUnit organizationalUnit);

    bool CanAddUserToOrganization(Organization organization);

    bool CanAddUserToOrganizationalUnit(OrganizationalUnit organizationalUnit);
}
