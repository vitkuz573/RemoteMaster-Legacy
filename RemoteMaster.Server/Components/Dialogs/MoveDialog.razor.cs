// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MoveDialog
{
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
                var unavailableHosts = new List<Computer>();

                foreach (var host in Hosts)
                {
                    if (host.Value != null)
                    {
                        await host.Value.InvokeAsync("ChangeGroup", targetGroup);
                    }
                    else
                    {
                        unavailableHosts.Add(host.Key);
                    }
                }

                if (unavailableHosts.Count != 0)
                {
                    await AppendGroupChangeRequests(unavailableHosts, targetGroup);
                }

                await DatabaseService.MoveNodesAsync(nodeIds, _selectedGroupId);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private static async Task AppendGroupChangeRequests(List<Computer> unavailableHosts, string targetGroup)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var applicationData = Path.Combine(programData, "RemoteMaster", "Server");

        if (!Directory.Exists(applicationData))
        {
            Directory.CreateDirectory(applicationData);
        }

        var groupChangeRequestsPath = Path.Combine(applicationData, "GroupChangeRequests.json");

        List<OrganizationalUnitChangeRequest> changeRequests;

        if (File.Exists(groupChangeRequestsPath))
        {
            var existingJson = await File.ReadAllTextAsync(groupChangeRequestsPath);
            changeRequests = JsonSerializer.Deserialize<List<OrganizationalUnitChangeRequest>>(existingJson) ?? [];
        }
        else
        {
            changeRequests = [];
        }

        foreach (var host in unavailableHosts)
        {
            var existingRequest = changeRequests.FirstOrDefault(r => r.MacAddress == host.MacAddress);

            if (existingRequest != null)
            {
                existingRequest.NewGroup = targetGroup;
            }
            else
            {
                changeRequests.Add(new OrganizationalUnitChangeRequest(host.MacAddress, targetGroup));
            }
        }

        var json = JsonSerializer.Serialize(changeRequests, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(groupChangeRequestsPath, json);
    }
}
