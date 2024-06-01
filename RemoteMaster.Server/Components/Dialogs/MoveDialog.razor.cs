// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using RemoteMaster.Server.Models;
using RemoteMaster.Shared.Models;

namespace RemoteMaster.Server.Components.Dialogs;

public partial class MoveDialog
{
    [Parameter]
    public EventCallback<IEnumerable<Computer>> OnNodesMoved { get; set; }

    private string _currentOrganizationName = string.Empty;
    private string _currentOrganizationalUnitName = string.Empty;
    private List<Organization> _organizations = [];
    private List<OrganizationalUnit> _organizationalUnits = [];
    private Guid _selectedOrganizationId = Guid.Empty;
    private Guid _selectedOrganizationalUnitId = Guid.Empty;

    protected async override Task OnInitializedAsync()
    {
        _organizations = [.. (await DatabaseService.GetNodesAsync<Organization>(node => node is Organization))];

        if (!Hosts.IsEmpty)
        {
            var firstHostParentId = Hosts.First().Key.ParentId;
            var currentOrganizationalUnit = await DatabaseService.GetNodesAsync<OrganizationalUnit>(node => node.NodeId == firstHostParentId);

            if (currentOrganizationalUnit.Any())
            {
                var currentOU = currentOrganizationalUnit.First();

                _selectedOrganizationalUnitId = currentOU.NodeId;
                _currentOrganizationalUnitName = currentOU.Name;

                var currentOrganization = _organizations.FirstOrDefault(org => org.NodeId == currentOU.OrganizationId);

                if (currentOrganization != null)
                {
                    _currentOrganizationName = currentOrganization.Name;
                    _selectedOrganizationId = currentOrganization.NodeId;
                }
            }
        }
    }

    private async Task OrganizationChanged(Guid organizationId)
    {
        _selectedOrganizationId = organizationId;
        _selectedOrganizationalUnitId = Guid.Empty;

        var organization = _organizations.FirstOrDefault(org => org.NodeId == organizationId);

        if (organization != null)
        {
            _organizationalUnits = [.. (await DatabaseService.GetNodesAsync<OrganizationalUnit>(node => node.OrganizationId == organizationId))];
        }

        StateHasChanged();
    }

    private async Task Move()
    {
        if (_selectedOrganizationalUnitId != Guid.Empty)
        {
            var targetOrganizationalUnitsPath = await DatabaseService.GetFullPathForOrganizationalUnitAsync(_selectedOrganizationalUnitId);

            if (targetOrganizationalUnitsPath.Length > 0)
            {
                var nodeIds = Hosts.Select(host => host.Key.NodeId);
                var unavailableHosts = new List<Computer>();

                foreach (var host in Hosts)
                {
                    if (host.Value != null)
                    {
                        await host.Value.InvokeAsync("ChangeOrganizationalUnit", targetOrganizationalUnitsPath);
                    }
                    else
                    {
                        unavailableHosts.Add(host.Key);
                    }
                }

                if (unavailableHosts.Count != 0)
                {
                    await AppendOrganizationalUnitChangeRequests(unavailableHosts, targetOrganizationalUnitsPath);
                }

                await DatabaseService.MoveNodesAsync(nodeIds, _selectedOrganizationalUnitId);
            }

            await OnNodesMoved.InvokeAsync(Hosts.Keys);

            MudDialog.Close(DialogResult.Ok(true));
        }
    }

    private static async Task AppendOrganizationalUnitChangeRequests(List<Computer> unavailableHosts, string[] targetOrganizationalUnits)
    {
        var programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var applicationData = Path.Combine(programDataPath, "RemoteMaster", "Server");

        if (!Directory.Exists(applicationData))
        {
            Directory.CreateDirectory(applicationData);
        }

        var ouChangeRequestsFilePath = Path.Combine(applicationData, "OrganizationalUnitChangeRequests.json");

        List<OrganizationalUnitChangeRequest> changeRequests;

        if (File.Exists(ouChangeRequestsFilePath))
        {
            var existingJson = await File.ReadAllTextAsync(ouChangeRequestsFilePath);
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
                existingRequest.NewOrganizationalUnit = targetOrganizationalUnits;
            }
            else
            {
                changeRequests.Add(new OrganizationalUnitChangeRequest(host.MacAddress, targetOrganizationalUnits));
            }
        }

        var json = JsonSerializer.Serialize(changeRequests, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(ouChangeRequestsFilePath, json);
    }
}