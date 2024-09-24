// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using MudBlazor;
using RemoteMaster.Server.Aggregates.OrganizationAggregate;
using Host = RemoteMaster.Server.Aggregates.OrganizationAggregate.Host;

namespace RemoteMaster.Server.Models;

public class UnifiedTreeItemData : TreeItemData<object>
{
    public UnifiedTreeItemData(object node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Value = node;

        Initialize(node);
    }

    private void Initialize(object node)
    {
        Children = [];

        switch (node)
        {
            case Organization organization:
                Text = organization.Name;
                Children.AddRange(organization.OrganizationalUnits.Select(unit => new UnifiedTreeItemData(unit) as TreeItemData<object>));
                break;

            case OrganizationalUnit unit:
                Text = unit.Name;
                Children.AddRange(unit.Hosts.Select(host => new UnifiedTreeItemData(host) as TreeItemData<object>));
                Children.AddRange(unit.Children.Select(child => new UnifiedTreeItemData(child) as TreeItemData<object>));
                break;

            case Host host:
                Text = host.Name;
                break;

            default:
                throw new InvalidOperationException($"Unsupported node type: {node.GetType().Name}");
        }
    }
}
