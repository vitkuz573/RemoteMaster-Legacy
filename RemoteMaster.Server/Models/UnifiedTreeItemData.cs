// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;
using RemoteMaster.Shared.Abstractions;

namespace RemoteMaster.Server.Models;

public class UnifiedTreeItemData : TreeItemData<INode>
{
    public UnifiedTreeItemData(INode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Value = node;
        Text = node.Name;
        Children = [];

        if (node is OrganizationalUnit unit)
        {
            if (unit.Computers != null)
            {
                Children.AddRange(unit.Computers.Select(computer => new UnifiedTreeItemData(computer) as TreeItemData<INode>));
            }

            if (unit.Children != null)
            {
                Children.AddRange(unit.Children.Select(child => new UnifiedTreeItemData(child) as TreeItemData<INode>));
            }
        }
        else if (node is Organization organization)
        {
            if (organization.OrganizationalUnits != null)
            {
                Children.AddRange(organization.OrganizationalUnits.Select(unit => new UnifiedTreeItemData(unit) as TreeItemData<INode>));
            }
        }
    }
}
