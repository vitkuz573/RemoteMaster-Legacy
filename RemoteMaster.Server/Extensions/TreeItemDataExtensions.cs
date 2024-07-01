// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Extensions;

public static class TreeItemDataExtensions
{
    public static TreeItemData<INode> ToTreeItemData(this Organization organization)
    {
        return new UnifiedTreeItemData(organization);
    }

    public static TreeItemData<INode> ToTreeItemData(this OrganizationalUnit unit)
    {
        return new UnifiedTreeItemData(unit);
    }

    public static TreeItemData<INode> ToTreeItemData(this Computer computer)
    {
        return new UnifiedTreeItemData(computer);
    }
}
