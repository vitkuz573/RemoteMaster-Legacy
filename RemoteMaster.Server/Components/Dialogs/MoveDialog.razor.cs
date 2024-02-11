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
    private string _currentOrganizationalUnitName = string.Empty;
    private List<OrganizationalUnit> _organizationalUnits = [];
    private Guid _selectedOrganizationalUnitId;

    protected async override Task OnInitializedAsync()
    {
        _organizationalUnits = (await DatabaseService.GetNodesAsync(node => node is OrganizationalUnit)).OfType<OrganizationalUnit>().ToList();

        if (!Hosts.IsEmpty)
        {
            var firstHostParentId = Hosts.First().Key.ParentId;
            var currentOrganizationalUnit = await DatabaseService.GetNodesAsync(node => node.NodeId == firstHostParentId);

            if (currentOrganizationalUnit.Any())
            {
                _selectedOrganizationalUnitId = currentOrganizationalUnit.First().NodeId;
                _currentOrganizationalUnitName = currentOrganizationalUnit.First().Name;
            }
        }
    }

    private async Task Move()
    {
        if (_selectedOrganizationalUnitId != Guid.Empty)
        {
            var targetOrganizationalUnit = _organizationalUnits.FirstOrDefault(ou => ou.NodeId == _selectedOrganizationalUnitId)?.Name;

            if (targetOrganizationalUnit != null)
            {
                var nodeIds = Hosts.Select(host => host.Key.NodeId);
                var unavailableHosts = new List<Computer>();

                foreach (var host in Hosts)
                {
                    if (host.Value != null)
                    {
                        await host.Value.InvokeAsync("ChangeOrganizationalUnit", targetOrganizationalUnit);
                    }
                    else
                    {
                        unavailableHosts.Add(host.Key);
                    }
                }

                if (unavailableHosts.Count != 0)
                {
                    await AppendOrganizationalUnitChangeRequests(unavailableHosts, targetOrganizationalUnit);
                }

                await DatabaseService.MoveNodesAsync(nodeIds, _selectedOrganizationalUnitId);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private static async Task AppendOrganizationalUnitChangeRequests(List<Computer> unavailableHosts, string targetOrganizationalUnit)
    {
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var applicationData = Path.Combine(programData, "RemoteMaster", "Server");

        if (!Directory.Exists(applicationData))
        {
            Directory.CreateDirectory(applicationData);
        }

        var ouChangeRequestsPath = Path.Combine(applicationData, "OrganizationalUnitChangeRequests.json");

        List<OrganizationalUnitChangeRequest> changeRequests;

        if (File.Exists(ouChangeRequestsPath))
        {
            var existingJson = await File.ReadAllTextAsync(ouChangeRequestsPath);
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
                existingRequest.NewOrganizationalUnit = targetOrganizationalUnit;
            }
            else
            {
                changeRequests.Add(new OrganizationalUnitChangeRequest(host.MacAddress, targetOrganizationalUnit));
            }
        }

        var json = JsonSerializer.Serialize(changeRequests, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ouChangeRequestsPath, json);
    }
}
