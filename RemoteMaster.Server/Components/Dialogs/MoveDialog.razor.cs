// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Abstractions;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MoveDialog
{
    [Inject]
    private IDatabaseService DatabaseService { get; set; } = default!;

    private string _currentGroupName = string.Empty;
    private List<Group> _groups = [];
    private Guid _selectedGroupId;

    protected async override Task OnInitializedAsync()
    {
        _groups = (await DatabaseService.GetNodesAsync(node => node is Group)).OfType<Group>().ToList();

        if (!Hosts.IsEmpty)
        {
            var firstHostParentId = Hosts.First().Key.ParentId;
            var currentGroup = await DatabaseService.GetNodesAsync(node => node.NodeId == firstHostParentId);

            if (currentGroup.Any())
            {
                _selectedGroupId = currentGroup.First().NodeId;
                _currentGroupName = currentGroup.First().Name;
            }
        }
    }

    private async Task Move()
    {
        if (_selectedGroupId != Guid.Empty)
        {
            var targetGroup = _groups.FirstOrDefault(g => g.NodeId == _selectedGroupId)?.Name;

            if (targetGroup != null)
            {
                var nodeIds = Hosts.Select(host => host.Key.NodeId);

                foreach (var host in Hosts)
                {
                    if (host.Value != null)
                    {
                        await host.Value.InvokeAsync("ChangeGroup", targetGroup);
                    }
                }

                await DatabaseService.MoveNodesAsync(nodeIds, _selectedGroupId);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
    }
}
