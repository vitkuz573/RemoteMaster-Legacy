// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using RemoteMaster.Server.Aggregates.OrganizationalUnitAggregate;

namespace RemoteMaster.Server.Models;

public class UnifiedTreeItemData : TreeItemData<object>
{
    public UnifiedTreeItemData(object node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Value = node;
        Children = [];

        switch (node)
        {
            case Organization organization:
                Text = organization.Name;
                if (organization.OrganizationalUnits != null)
                {
                    Children.AddRange(organization.OrganizationalUnits.Select(unit => new UnifiedTreeItemData(unit) as TreeItemData<object>));
                }
                break;

            case OrganizationalUnit unit:
                Text = unit.Name;
                
                if (unit.Computers != null)
                {
                    Children.AddRange(unit.Computers.Select(computer => new UnifiedTreeItemData(computer) as TreeItemData<object>));
                }
                
                if (unit.Children != null)
                {
                    Children.AddRange(unit.Children.Select(child => new UnifiedTreeItemData(child) as TreeItemData<object>));
                }
                break;

            case Computer computer:
                Text = computer.Name;
                break;

            default:
                throw new InvalidOperationException($"Unsupported node type: {node.GetType().Name}");
        }
    }
}
